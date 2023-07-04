namespace ReflectiveCode.DockerMonitor;

public readonly struct DockerContainerState
{
    public DockerContainerHealth Health { get; init; }
    public DockerStatus Status { get; init; }
    public int ExitCode { get; init; }
    public bool OOMKilled { get; init; }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + Health.GetHashCode();
            hash = hash * 23 + Status.GetHashCode();
            hash = hash * 23 + ExitCode.GetHashCode();
            hash = hash * 23 + OOMKilled.GetHashCode();
            return hash;
        }
    }
}
