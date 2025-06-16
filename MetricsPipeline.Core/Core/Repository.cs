namespace MetricsPipeline.Core;

/// <summary>
/// Minimal repository abstraction used by the unit of work.
/// </summary>
/// <typeparam name="TEntity">Entity type.</typeparam>
public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Retrieves an entity by its identifier.
    /// </summary>
    Task<TEntity?> GetByIdAsync(object id, CancellationToken ct = default);

    /// <summary>
    /// Adds an entity to the context.
    /// </summary>
    Task AddAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>
    /// Removes an entity from the context.
    /// </summary>
    void Remove(TEntity entity);
}

/// <summary>
/// Provides repositories and coordinates saving changes.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Gets a repository for the specified entity type.
    /// </summary>
    IRepository<TEntity> Repository<TEntity>() where TEntity : class;

    /// <summary>
    /// Persists pending changes to the underlying store.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
