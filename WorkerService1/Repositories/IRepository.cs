using ExampleLib.Infrastructure;
using ExampleLib.Domain;

namespace WorkerService1.Repositories
{
    /// <summary>
    /// Repository interface for WorkerService1 entities.
    /// Shows how to integrate ExampleLib validation into existing repository patterns.
    /// </summary>
    public interface IRepository<T> where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<List<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task AddManyAsync(IEnumerable<T> entities);
        Task UpdateAsync(T entity);
        Task UpdateManyAsync(IEnumerable<T> entities);
        Task DeleteAsync(int id);
        Task<bool> ValidateAsync(T entity, CancellationToken cancellationToken = default);
        Task<bool> ValidateManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task<T?> GetLastAsync();
    }
}
