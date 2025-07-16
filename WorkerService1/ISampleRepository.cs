using System.Collections.Generic;
using System.Threading.Tasks;

namespace WorkerService1.Repositories
{
    public interface ISampleRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<List<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
    }
}