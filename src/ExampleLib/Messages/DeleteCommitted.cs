namespace ExampleLib.Messages;

/// <summary>
/// Event published when a delete operation has been committed.
/// </summary>
public class DeleteCommitted<T>
{
    public string AppName { get; }
    public string EntityType { get; }
    public string EntityId { get; }
    public T? Payload { get; }

    public DeleteCommitted(string appName, string entityType, string entityId, T? payload)
    {
        AppName = appName;
        EntityType = entityType;
        EntityId = entityId;
        Payload = payload;
    }
}
