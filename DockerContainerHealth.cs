namespace ReflectiveCode.DockerMonitor;

public readonly struct DockerContainerHealth
{
    public DockerHealth Status { get; init; }

    public override int GetHashCode()
    {
        return Status.GetHashCode();
    }
}
