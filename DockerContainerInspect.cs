namespace ReflectiveCode.DockerMonitor;

public class DockerContainerInspect
{
    public string Id { get; init; } = "NOT_SET";
    public string Name { get; init; } = "NOT_SET";
    public int RestartCount { get; init; }
    public DockerContainerState State { get; init; }

    public string NameDisplay => Name.TrimStart('/');
    public string StateDisplay => State.Status.ToString().ToLowerInvariant();
    public string HealthDisplay => State.Health.Status.ToString().ToLowerInvariant();


    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + Id.GetHashCode();
            hash = hash * 23 + Name.GetHashCode();
            hash = hash * 23 + RestartCount.GetHashCode();
            hash = hash * 23 + State.GetHashCode();
            return hash;
        }
    }
}
