namespace ExampleLib.Domain;

/// <summary>
/// Runs all registered validation services for an entity instance.
/// </summary>
public interface IValidationRunner
{
    /// <summary>
    /// Validate and audit the specified entity.
    /// Returns <c>true</c> when all validation services succeed.
    /// </summary>
    Task<bool> ValidateAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : IValidatable, IBaseEntity, IRootEntity;

    /// <summary>
    /// Validate and audit the specified collection of entities.
    /// Returns <c>true</c> when all validation services succeed for all entities.
    /// </summary>
    Task<bool> ValidateManyAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        where T : IValidatable, IBaseEntity, IRootEntity;
}
