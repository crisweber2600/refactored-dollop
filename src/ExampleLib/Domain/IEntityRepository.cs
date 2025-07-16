namespace ExampleLib.Domain;

/// <summary>
/// Repository interface for entity operations. Saving an entity will publish a SaveRequested event.
/// </summary>
public interface IEntityRepository<T>
{
    /// <summary>
    /// Saves the entity and publishes a <see cref="SaveRequested{T}"/> event.
    /// </summary>
    /// <param name="entity">The entity to save.</param>
    /// <param name="appName">Optional application name. When omitted the entry assembly name is used.</param>
    Task SaveAsync(T entity, string? appName = null);
}
