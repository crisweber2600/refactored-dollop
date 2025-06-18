namespace ExampleLib.Domain;

/// <summary>
/// Event published after an entity save has been validated.
/// </summary>
public class SaveValidated<T>
{
    public string AppName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public T? Payload { get; set; }
    public bool Validated { get; set; }
}
