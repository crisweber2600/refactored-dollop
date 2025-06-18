using System.Threading.Tasks;

namespace ExampleLib.Domain;

/// <summary>
/// Repository interface for entity operations. Saving an entity will publish a SaveRequested event.
/// </summary>
public interface IEntityRepository<T>
{
    /// <summary>
    /// Saves the entity (publishing a SaveRequested event for validation).
    /// </summary>
    Task SaveAsync(string appName, T entity);
}
