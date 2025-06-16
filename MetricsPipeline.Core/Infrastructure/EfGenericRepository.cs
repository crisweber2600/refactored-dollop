using MetricsPipeline.Core;
using Microsoft.EntityFrameworkCore;

namespace MetricsPipeline.Infrastructure;

/// <summary>
/// Generic EF Core repository implementing <see cref="IGenericRepository{T}"/>.
/// </summary>
public class EfGenericRepository<TEntity> : IGenericRepository<TEntity>
    where TEntity : class, ISoftDelete, IBaseEntity, IRootEntity
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _set;
    private readonly bool _allowHardDelete;

    /// <inheritdoc />
    public bool IgnoreSoftDeleteFilter { get; set; }

    /// <summary>
    /// Initializes a new repository instance.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="allowHardDelete">Allow hard deletions when true.</param>
    public EfGenericRepository(DbContext context, bool allowHardDelete = false)
    {
        _context = context;
        _allowHardDelete = allowHardDelete;
        _set = context.Set<TEntity>();
    }

    /// <inheritdoc />
    public async Task AddAsync(TEntity entity)
        => await _set.AddAsync(entity);

    /// <inheritdoc />
    public async Task<int> CreateAsync(TEntity entity)
    {
        await _set.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<TEntity> entities)
        => await _set.AddRangeAsync(entities);

    /// <inheritdoc />
    public void Delete(TEntity entity)
    {
        entity.IsDeleted = true;
        _set.Update(entity);
    }

    /// <inheritdoc />
    public async Task<int> DeleteAsync(TEntity entity, bool hardDelete)
    {
        if (hardDelete)
        {
            if (!_allowHardDelete)
                throw new HardDeleteNotPermittedException();
            _set.Remove(entity);
        }
        else
        {
            entity.IsDeleted = true;
            _set.Update(entity);
        }
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    /// <inheritdoc />
    public void DeleteRange(IEnumerable<TEntity> entities)
    {
        foreach (var e in entities)
            e.IsDeleted = true;
        _set.UpdateRange(entities);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(params string[] includeStrings)
    {
        IQueryable<TEntity> query = IgnoreSoftDeleteFilter
            ? _set.IgnoreQueryFilters()
            : _set;
        foreach (var include in includeStrings)
            query = query.Include(include);
        return await query.ToListAsync();
    }

    /// <inheritdoc />
    public async Task<TEntity?> GetByIdAsync(int id, params string[] includeStrings)
    {
        IQueryable<TEntity> query = IgnoreSoftDeleteFilter
            ? _set.IgnoreQueryFilters()
            : _set;
        query = query.Where(e => e.Id == id);
        foreach (var include in includeStrings)
            query = query.Include(include);
        return await query.FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(ISpecification<TEntity>? specification = null)
    {
        var query = IgnoreSoftDeleteFilter ? _set.IgnoreQueryFilters() : _set.AsQueryable();
        if (specification?.Criteria != null)
            query = query.Where(specification.Criteria);
        return await query.CountAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> SearchAsync(ISpecification<TEntity> specification)
    {
        var query = IgnoreSoftDeleteFilter ? _set.IgnoreQueryFilters() : _set.AsQueryable();
        if (specification.Criteria != null)
            query = query.Where(specification.Criteria);
        return await query.ToListAsync();
    }

    /// <inheritdoc />
    public void Update(TEntity entity)
        => _set.Update(entity);

    /// <inheritdoc />
    public async Task<int> UpdateAsync(TEntity entity)
    {
        _set.Update(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    /// <inheritdoc />
    public void UpdateRange(IEnumerable<TEntity> entities)
        => _set.UpdateRange(entities);
}
