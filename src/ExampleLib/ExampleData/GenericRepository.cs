using Microsoft.EntityFrameworkCore;

namespace ExampleData;

public interface IGenericRepository<T>
    where T : class, IValidatable, IBaseEntity, IRootEntity
{
    Task<T?> GetByIdAsync(int id, bool includeDeleted = false);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task AddManyAsync(IEnumerable<T> entities);
    /// <summary>
    /// Deletes the given entity. When <paramref name="hardDelete"/> is
    /// <c>false</c>, the entity is soft deleted by unvalidating it
    /// (setting <see cref="IValidatable.Validated"/> to <c>false</c>)
    /// instead of toggling an <c>IsDeleted</c> flag.
    /// </summary>
    Task DeleteAsync(T entity, bool hardDelete = false);
    Task<int> CountAsync();
}

public class EfGenericRepository<T> : IGenericRepository<T>
    where T : class, IValidatable, IBaseEntity, IRootEntity
{
    private readonly YourDbContext _context;
    private readonly DbSet<T> _set;

    public EfGenericRepository(YourDbContext context)
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

    public async Task AddManyAsync(IEnumerable<T> entities)
    {
        await _set.AddRangeAsync(entities);
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
