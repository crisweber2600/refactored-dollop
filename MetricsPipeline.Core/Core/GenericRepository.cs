namespace MetricsPipeline.Core;

/// <summary>
/// Generic repository with soft delete support.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public interface IGenericRepository<T>
    where T : class, ISoftDelete, IBaseEntity, IRootEntity
{
    /// <summary>
    /// Gets or sets whether the global soft delete query filter should be ignored.
    /// </summary>
    bool IgnoreSoftDeleteFilter { get; set; }

    /// <summary>
    /// Persists a new entity and returns its identifier.
    /// </summary>
    Task<int> CreateAsync(T entity);

    /// <summary>
    /// Updates an existing entity and saves the changes.
    /// </summary>
    Task<int> UpdateAsync(T entity);

    /// <summary>
    /// Deletes an entity. Can perform a hard delete when allowed.
    /// </summary>
    /// <param name="entity">Entity to delete.</param>
    /// <param name="hardDelete">Whether to remove the entity permanently.</param>
    Task<int> DeleteAsync(T entity, bool hardDelete);

    /// <summary>
    /// Adds an entity to the context without saving.
    /// </summary>
    Task AddAsync(T entity);

    /// <summary>
    /// Adds multiple entities to the context without saving.
    /// </summary>
    Task AddRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// Retrieves all entities including specified relations.
    /// </summary>
    Task<IReadOnlyList<T>> GetAllAsync(params string[] includeStrings);

    /// <summary>
    /// Retrieves an entity by identifier.
    /// </summary>
    Task<T?> GetByIdAsync(int id, params string[] includeStrings);

    /// <summary>
    /// Searches for entities matching the specification.
    /// </summary>
    Task<IReadOnlyList<T>> SearchAsync(ISpecification<T> specification);

    /// <summary>
    /// Counts entities optionally filtered by a specification.
    /// </summary>
    Task<int> GetCountAsync(ISpecification<T>? specification = null);

    /// <summary>
    /// Marks an entity as updated in the context.
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Marks a collection of entities as updated in the context.
    /// </summary>
    void UpdateRange(IEnumerable<T> entities);

    /// <summary>
    /// Soft deletes an entity in the context.
    /// </summary>
    void Delete(T entity);

    /// <summary>
    /// Soft deletes a range of entities in the context.
    /// </summary>
    void DeleteRange(IEnumerable<T> entities);
}
