namespace ExampleLib.Messages;

/// <summary>
/// Event published after a delete request has been validated.
/// </summary>
public class DeleteValidated<T>
{
    public string AppName { get; }
    public string EntityType { get; }
    public string EntityId { get; }
    public T? Payload { get; }
    public bool Validated { get; }

    public DeleteValidated(string appName, string entityType, string entityId, T? payload, bool validated)
    {
        AppName = appName;
        EntityType = entityType;
        EntityId = entityId;
        Payload = payload;
        Validated = validated;
    }
}
