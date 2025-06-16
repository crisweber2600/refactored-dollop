namespace MetricsPipeline.Core;

public interface IGenericRepository<T>
    where T : class, ISoftDelete, IBaseEntity, IRootEntity
{
    Task<int> CreateAsync(T entity);
    Task<int> UpdateAsync(T entity);
    Task<int> DeleteAsync(T entity, bool hardDelete);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    Task<IReadOnlyList<T>> GetAllAsync(params string[] includeStrings);
    Task<T?> GetByIdAsync(int id, params string[] includeStrings);
    Task<IReadOnlyList<T>> SearchAsync(ISpecification<T> specification);
    Task<int> GetCountAsync(ISpecification<T>? specification = null);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);
}
