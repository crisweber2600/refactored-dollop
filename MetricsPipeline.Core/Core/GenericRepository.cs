namespace MetricsPipeline.Core;

public interface IGenericRepository<T>
    where T : class, ISoftDelete, IBaseEntity, IRootEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>> SearchAsync(ISpecification<T> specification, CancellationToken ct = default);
    Task<int> GetCountAsync(ISpecification<T>? specification = null, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);
}
