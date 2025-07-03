namespace ExampleLib.Messages;

/// <summary>
/// Event published when a delete operation is requested for an entity.
/// </summary>
public class DeleteRequested<T>
{
    public string AppName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public T? Payload { get; set; }
}
