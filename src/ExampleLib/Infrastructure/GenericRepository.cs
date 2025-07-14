using Microsoft.EntityFrameworkCore;
using ExampleLib.Domain;

namespace ExampleLib.Infrastructure;

public class EfGenericRepository<T> : IGenericRepository<T>
    where T : class, IValidatable, IBaseEntity, IRootEntity
{
    private readonly DbContext _context;
    private readonly DbSet<T> _set;

    public EfGenericRepository(DbContext context)
    {
        _context = context;
        _set = _context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id, bool includeDeleted = false)
    {
        var query = includeDeleted ? _set.IgnoreQueryFilters() : _set;
        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public Task<List<T>> GetAllAsync() => _set.ToListAsync();

    public Task AddAsync(T entity) => _set.AddAsync(entity).AsTask();

    public Task AddManyAsync(IEnumerable<T> entities) => _set.AddRangeAsync(entities);

    public Task UpdateAsync(T entity)
    {
        _set.Update(entity);
        return Task.CompletedTask;
    }

    public Task UpdateManyAsync(IEnumerable<T> entities)
    {
        _set.UpdateRange(entities);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(T entity, bool hardDelete = false)
    {
        if (hardDelete)
            _set.Remove(entity);
        else
        {
            entity.Validated = false;
            _set.Update(entity);
        }
        await _context.SaveChangesAsync();
    }

    public Task<int> CountAsync() => _set.CountAsync();
}
