namespace ReflectiveCode.DockerMonitor;

public class ContainerMessage
{
    public int Priority { get; }
    public string ContainerName { get; }
    public string Message { get; }
    public bool Error { get; }

    public ContainerMessage(DockerContainerInspect container)
    {
        ContainerName = container.NameDisplay;

        switch (container.State.Status)
        {
            case DockerStatus.Created:
                Message = $":large_blue_circle: *{container.NameDisplay}* • `{container.StateDisplay}`";
                Priority = 1;
                return;
            case DockerStatus.Running when container.State.Health.Status == DockerHealth.Starting:
                Message = $":large_blue_circle: *{container.NameDisplay}* • `{container.HealthDisplay}`";
                if (container.RestartCount > 0)
                    Message += $" • restart count `{container.RestartCount}`";
                Priority = 1;
                return;
            case DockerStatus.Running when container.State.Health.Status == DockerHealth.Unhealthy:
                Message = $":red_circle: *{container.NameDisplay}* • `{container.HealthDisplay}`";
                if (container.RestartCount > 0)
                    Message += $" • restart count `{container.RestartCount}`";
                Priority = 3;
                Error = true;
                return;
            case DockerStatus.Running when container.State.Health.Status == DockerHealth.Healthy:
                Message = $":large_green_circle: *{container.NameDisplay}* • `{container.HealthDisplay}`";
                if (container.RestartCount > 0)
                    Message += $" • restart count `{container.RestartCount}`";
                return;
            case DockerStatus.Running:
                Message = $":large_green_circle: *{container.NameDisplay}* • `{container.StateDisplay}`";
                if (container.RestartCount > 0)
                    Message += $" • restart count `{container.RestartCount}`";
                return;
            case DockerStatus.Paused:
                Message = $":white_circle: *{container.NameDisplay}* • `{container.StateDisplay}`";
                Priority = 1;
                return;
            case DockerStatus.Restarting when container.State.OOMKilled:
                Message = $":skull_and_crossbones: *{container.NameDisplay}* • `{container.StateDisplay}` • `OOMKilled` • exit code `{container.State.ExitCode}` • restart count `{container.RestartCount}`";
                Priority = 3;
                Error = true;
                return;
            case DockerStatus.Restarting:
                Message = $":red_circle: *{container.NameDisplay}* • `{container.StateDisplay}` • exit code `{container.State.ExitCode}` • restart count `{container.RestartCount}`";
                Priority = 3;
                Error = true;
                return;
            case DockerStatus.Removing:
                Message = $":black_circle: *{container.NameDisplay}* • `{container.StateDisplay}`";
                Priority = 1;
                return;
            case DockerStatus.Exited when container.State.OOMKilled:
                Message = $":skull_and_crossbones: *{container.NameDisplay}* • `{container.StateDisplay}` • `OOMKilled` • exit code `{container.State.ExitCode}` • restart count `{container.RestartCount}`";
                Priority = 3;
                Error = true;
                return;
            case DockerStatus.Exited when container.State.ExitCode == 0:
                Message = $":large_yellow_circle: *{container.NameDisplay}* • `{container.StateDisplay}` • exit code `{container.State.ExitCode}` • restart count `{container.RestartCount}`";
                Priority = 2;
                return;
            case DockerStatus.Exited:
                Message = $":red_circle: *{container.NameDisplay}* • `{container.StateDisplay}` • exit code `{container.State.ExitCode}` • restart count `{container.RestartCount}`";
                Priority = 3;
                Error = true;
                return;
            case DockerStatus.Dead:
                Message = $":red_circle: *{container.NameDisplay}* • `{container.StateDisplay}`";
                Priority = 3;
                Error = true;
                return;
            default:
                Message = $":red_circle: *{container.NameDisplay}* • `{container.StateDisplay}`";
                Priority = 3;
                Error = true;
                return;
        }
    }
}
