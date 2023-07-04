using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NCrontab;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ReflectiveCode.DockerMonitor;

public class Program
{
    private static string DockerSocket = "/var/run/docker.sock";
    private static string? HeartbeatUrl;
    private static string Host = "Docker";
    private static LogLevel LogLevel = LogLevel.Info;
    private static CrontabSchedule? Schedule;
    private static string SlackWebhookUrl = "";
    private static int Timeout = 10;

    public static async Task<int> Main(string[] args)
    {
        using var cts = new CancellationTokenSource();
        EventHandler handler = (sender, e) => cts.Cancel();
        AppDomain.CurrentDomain.ProcessExit += handler;
        try
        {
            var parseOptions = new CrontabSchedule.ParseOptions { IncludingSeconds = true };

            DockerSocket = Environment.GetEnvironmentVariable("DOCKER_SOCKET") ?? DockerSocket;
            HeartbeatUrl = Environment.GetEnvironmentVariable("HEARTBEAT_URL");
            Host = Environment.GetEnvironmentVariable("HOST") ?? Environment.MachineName;
            LogLevel = Enum.Parse<LogLevel>(Environment.GetEnvironmentVariable("LOG_LEVEL") ?? LogLevel.ToString());
            Schedule = CrontabSchedule.Parse(Environment.GetEnvironmentVariable("SCHEDULE") ?? "0 * * * * *", parseOptions);
            SlackWebhookUrl = Environment.GetEnvironmentVariable("SLACK_WEBHOOK_URL") ?? throw new Exception("Missing SLACK_WEBHOOK_URL environment variable");
            Timeout = Int32.Parse(Environment.GetEnvironmentVariable("TIMEOUT") ?? Timeout.ToString());

            LogInfo("Startup");

            var hashCode = 0;

            while (true)
            {
                var nextTime = Schedule.GetNextOccurrence(DateTime.Now);

                var delay = (nextTime - DateTime.Now);
                if (delay > TimeSpan.Zero)
                {
                    LogDebug($"Waiting {delay} until {nextTime}");
                    await Task.Delay(delay, cts.Token);
                }

                hashCode = await MonitorAsync(hashCode, cts.Token);
                if (cts.IsCancellationRequested) return 0;

            }
        }
        catch (TaskCanceledException) when (cts.IsCancellationRequested)
        {
            return 0;
        }
        catch (Exception e)
        {
            LogError("Fatal error");
            LogError(e.Message);
            LogError(e.ToString());
            return 1;
        }
        finally
        {
            AppDomain.CurrentDomain.ProcessExit -= handler;
        }
    }

    private static async Task<int> MonitorAsync(int hashCode, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(Timeout),
        };
        using var dockerClient = new HttpClient(new SocketsHttpHandler
        {
            ConnectCallback = async (context, token) =>
            {
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                var endpoint = new UnixDomainSocketEndPoint(DockerSocket);
                await socket.ConnectAsync(endpoint);
                return new NetworkStream(socket, ownsSocket: true);
            }
        })
        {
            Timeout = TimeSpan.FromSeconds(Timeout),
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var listUrl = $"http://localhost/v1.41/containers/json?all=true&filters={Uri.EscapeDataString("{\"label\":[\"com.reflectivecode.dockermonitor.enable=true\"]}")}";
        var containerList = await GetAsync<IReadOnlyList<DockerContainerList>>(dockerClient, listUrl, cancellationToken);
        LogDebug($"Listed {containerList.Count} containers");

        var containers = new List<DockerContainerInspect>();

        foreach (var containerId in containerList.OrderBy(x => x.Id))
        {
            if (String.IsNullOrEmpty(containerId.Id))
            {
                LogWarn("Missing container id. Cannot inspect.");
                continue;
            }

            var inspectUrl = $"http://localhost/v1.41/containers/{Uri.EscapeDataString(containerId.Id)}/json";
            try
            {
                var container = await GetAsync<DockerContainerInspect>(dockerClient, inspectUrl, cancellationToken);
                LogDebug($"Inspected {containerId.Id} {container.NameDisplay} container");
                containers.Add(container);
            }
            catch (HttpRequestException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                LogWarn($"Container {containerId.Id} not found.");
            }
        }

        LogDebug($"Checked docker in {stopwatch.Elapsed}");

        var newHashCode = GetHashCode(containers);
        if (newHashCode != hashCode)
        {
            var messages = containers
                .Select(container => new ContainerMessage(container))
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.ContainerName)
                .ToList();

            var containerCount = messages.Count;
            var errorCount = messages.Count(x => x.Error);
            var headerMessage = (errorCount == 0)
                ? $"Monitoring {containerCount} containers on {Host}"
                : $":warning: {errorCount} of {containerCount} containers have errors on {Host}";

            var payload = new JObject(new JProperty("blocks", new JArray(messages
                .Select(x => new JObject(
                    new JProperty("type", "section"),
                    new JProperty("text", new JObject(
                        new JProperty("type", "mrkdwn"),
                        new JProperty("text", x.Message)
                    ))
                ))
                .Prepend(new JObject(
                    new JProperty("type", "header"),
                    new JProperty("text", new JObject(
                        new JProperty("type", "plain_text"),
                        new JProperty("text", headerMessage),
                        new JProperty("emoji", true)
                    ))
                ))
            )));

            await PostJson(httpClient, SlackWebhookUrl, payload.ToString(), cancellationToken);
        }

        stopwatch.Stop();
        LogDebug($"Completed monitoring in {stopwatch.Elapsed}");

        if (!String.IsNullOrEmpty(HeartbeatUrl))
        {
            var url = HeartbeatUrl.Replace("{milliseconds}", stopwatch.ElapsedMilliseconds.ToString());
            try
            {
                await GetAsync(httpClient, url, cancellationToken);
            }
            catch (Exception e)
            {
                LogWarn("Failed to make heartbeat");
                LogError(e.Message);
                LogError(e.ToString());
            }
        }

        return newHashCode;
    }

    private static int GetHashCode(IReadOnlyCollection<DockerContainerInspect> containers)
    {
        unchecked
        {
            int hash = 17;
            foreach (var container in containers)
                hash = hash * 23 + container.GetHashCode();
            return hash;
        }
    }

    private static async Task<T> GetAsync<T>(HttpClient client, string url, CancellationToken cancellationToken)
    {
        LogDebug($"Get {url}");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        using var responseMessage = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken);
        var responseString = await responseMessage.Content.ReadAsStringAsync();
        try
        {
            responseMessage.EnsureSuccessStatusCode();
        }
        catch
        {
            LogError($"Failed GET request to {url}");
            LogError($"Response code {responseMessage.StatusCode} and body {responseString}");
            throw;
        }
        return JsonConvert.DeserializeObject<T>(responseString) ?? throw new Exception("response object is null");
    }

    private static async Task PostJson(HttpClient client, string url, string body, CancellationToken cancellationToken)
    {
        LogDebug($"Post url {url}");
        LogDebug($"Post body {body}");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");
        using var responseMessage = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken);
        var responseString = await responseMessage.Content.ReadAsStringAsync();
        try
        {
            responseMessage.EnsureSuccessStatusCode();
        }
        catch
        {
            LogError($"Failed POST request to {url}");
            LogError($"Response code {responseMessage.StatusCode} and body {responseString}");
            throw;
        }
    }

    private static async Task GetAsync(HttpClient client, string url, CancellationToken cancellationToken)
    {
        await Retry(3, async () =>
        {
            LogDebug($"Get {url}");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            using var responseMessage = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken);
            var responseString = await responseMessage.Content.ReadAsStringAsync();
            try
            {
                responseMessage.EnsureSuccessStatusCode();
            }
            catch
            {
                LogError($"Failed GET request to {url}");
                LogError($"Response code {responseMessage.StatusCode} and body {responseString}");
                throw;
            }
        });
    }

    private static void LogError(string message) => Log(LogLevel.Error, message);
    private static void LogWarn(string message) => Log(LogLevel.Warn, message);
    private static void LogInfo(string message) => Log(LogLevel.Info, message);
    private static void LogDebug(string message) => Log(LogLevel.Debug, message);

    private static void Log(LogLevel level, string message)
    {
        if (level > LogLevel) return;
        Console.Write(DateTime.Now.ToString("s"));
        Console.Write(level switch
        {
            LogLevel.Error => " ERROR ",
            LogLevel.Warn => " WARN  ",
            LogLevel.Info => " INFO  ",
            LogLevel.Debug => " DEBUG ",
            _ => $" {(int)level} ",
        });
        Console.WriteLine(message);
    }

    private static async Task Retry(int maxAttempts, Func<Task> func)
    {
        for (var i = 1; true; i++)
        {
            try
            {
                await func();
                return;
            }
            catch when (i < maxAttempts)
            {
                LogDebug($"Attempt {i} failed. Starting attempt {i + 1} of {maxAttempts}");
            }
            catch
            {
                LogDebug($"Attempt {i} failed. No more attempts will be made.");
                throw;
            }
        }
    }
}
