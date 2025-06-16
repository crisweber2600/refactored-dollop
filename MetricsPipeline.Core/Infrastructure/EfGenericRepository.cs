using MetricsPipeline.Core;
using Microsoft.EntityFrameworkCore;

namespace MetricsPipeline.Infrastructure;

public class EfGenericRepository<TEntity> : IGenericRepository<TEntity>
    where TEntity : class, ISoftDelete, IBaseEntity, IRootEntity
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _set;

    public EfGenericRepository(DbContext context)
    {
        _context = context;
        _set = context.Set<TEntity>();
    }

    public async Task AddAsync(TEntity entity)
        => await _set.AddAsync(entity);

    public async Task AddRangeAsync(IEnumerable<TEntity> entities)
        => await _set.AddRangeAsync(entities);

    public void Delete(TEntity entity)
    {
        entity.IsDeleted = true;
        _set.Update(entity);
    }

    public void DeleteRange(IEnumerable<TEntity> entities)
    {
        foreach (var e in entities)
        {
            e.IsDeleted = true;
        }
        _set.UpdateRange(entities);
    }

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(params string[] includeStrings)
    {
        IQueryable<TEntity> query = _set.Where(e => !e.IsDeleted);
        foreach (var include in includeStrings)
            query = query.Include(include);
        return await query.ToListAsync();
    }

    public async Task<TEntity?> GetByIdAsync(int id, params string[] includeStrings)
    {
        IQueryable<TEntity> query = _set.Where(e => e.Id == id && !e.IsDeleted);
        foreach (var include in includeStrings)
            query = query.Include(include);
        return await query.FirstOrDefaultAsync();
    }

    public async Task<int> GetCountAsync(ISpecification<TEntity>? specification = null)
    {
        var query = _set.AsQueryable().Where(e => !e.IsDeleted);
        if (specification?.Criteria != null)
            query = query.Where(specification.Criteria);
        return await query.CountAsync();
    }

    public async Task<IReadOnlyList<TEntity>> SearchAsync(ISpecification<TEntity> specification)
    {
        var query = _set.AsQueryable().Where(e => !e.IsDeleted);
        if (specification.Criteria != null)
            query = query.Where(specification.Criteria);
        return await query.ToListAsync();
    }

    public void Update(TEntity entity)
        => _set.Update(entity);

    public void UpdateRange(IEnumerable<TEntity> entities)
        => _set.UpdateRange(entities);
}
