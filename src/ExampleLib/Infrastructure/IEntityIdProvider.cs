namespace ExampleLib.Infrastructure;

/// <summary>
/// Provides entity ID values for use in validation and audit scenarios.
/// </summary>
public interface IEntityIdProvider
{
    /// <summary>
    /// Get the ID value for the specified entity instance.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="entity">The entity instance</param>
    /// <returns>The ID value as a string</returns>
    string GetEntityId<T>(T entity);
}