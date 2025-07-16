using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerService1.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<List<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
        Task<bool> ValidateAsync(T entity, CancellationToken cancellationToken = default);
    }
}
