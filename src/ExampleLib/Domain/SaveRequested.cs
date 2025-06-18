namespace ExampleLib.Domain;

/// <summary>
/// Event published when a save operation is requested for an entity of type T.
/// </summary>
public class SaveRequested<T>
{
    public string AppName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public T? Payload { get; set; }
}
