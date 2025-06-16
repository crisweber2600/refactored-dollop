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

    public async Task AddAsync(TEntity entity, CancellationToken ct = default)
        => await _set.AddAsync(entity, ct);

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
        => await _set.AddRangeAsync(entities, ct);

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

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default)
        => await _set.Where(e => !e.IsDeleted).ToListAsync(ct);

    public async Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, ct);

    public async Task<int> GetCountAsync(ISpecification<TEntity>? specification = null, CancellationToken ct = default)
    {
        var query = _set.AsQueryable().Where(e => !e.IsDeleted);
        if (specification?.Criteria != null)
            query = query.Where(specification.Criteria);
        return await query.CountAsync(ct);
    }

    public async Task<IReadOnlyList<TEntity>> SearchAsync(ISpecification<TEntity> specification, CancellationToken ct = default)
    {
        var query = _set.AsQueryable().Where(e => !e.IsDeleted);
        if (specification.Criteria != null)
            query = query.Where(specification.Criteria);
        return await query.ToListAsync(ct);
    }

    public void Update(TEntity entity)
        => _set.Update(entity);

    public void UpdateRange(IEnumerable<TEntity> entities)
        => _set.UpdateRange(entities);
}
