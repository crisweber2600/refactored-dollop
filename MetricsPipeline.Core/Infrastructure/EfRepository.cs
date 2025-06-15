namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;
using Microsoft.EntityFrameworkCore;

public class EfRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _set;

    public EfRepository(DbContext context)
    {
        _context = context;
        _set = context.Set<TEntity>();
    }

    public Task<TEntity?> GetByIdAsync(object id, CancellationToken ct = default)
        => _set.FindAsync([id], ct).AsTask();

    public Task AddAsync(TEntity entity, CancellationToken ct = default)
        => _set.AddAsync(entity, ct).AsTask();

    public void Remove(TEntity entity) => _set.Remove(entity);
}
