namespace ExampleLib.Domain;

/// <summary>
/// Event published after an entity save has been validated.
/// </summary>
public class SaveValidated<T>
{
    public string AppName { get; }
    public string EntityType { get; }
    public string EntityId { get; }
    public T? Payload { get; }
    public bool Validated { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveValidated{T}"/> class.
    /// </summary>
    /// <param name="appName">The name of the application.</param>
    /// <param name="entityType">The type of the entity.</param>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="payload">The payload associated with the entity.</param>
    /// <param name="validated">Indicates whether the entity has been validated.</param>
    public SaveValidated(string appName, string entityType, string entityId, T? payload, bool validated)
    {
        AppName = appName;
        EntityType = entityType;
        EntityId = entityId;
        Payload = payload;
        Validated = validated;
    }
}
