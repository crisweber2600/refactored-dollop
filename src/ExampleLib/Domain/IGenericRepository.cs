using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExampleLib.Domain;

public interface IGenericRepository<T>
    where T : class, IValidatable, IBaseEntity, IRootEntity
{
    Task<T?> GetByIdAsync(int id, bool includeDeleted = false);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task AddManyAsync(IEnumerable<T> entities);
    Task UpdateAsync(T entity);
    Task UpdateManyAsync(IEnumerable<T> entities);
    Task DeleteAsync(T entity, bool hardDelete = false);
    Task<int> CountAsync();
}
