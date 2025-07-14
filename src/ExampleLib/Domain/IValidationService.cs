namespace ExampleLib.Domain;

/// <summary>
/// Validates an entity against the configured summarisation plan and records the audit.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validate the entity and persist an audit entry.
    /// Returns <c>true</c> when the entity passes validation.
    /// </summary>
    Task<bool> ValidateAndSaveAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : IValidatable, IBaseEntity, IRootEntity;
}
