namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Basic EF Core repository implementation.
/// </summary>
public class EfRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _set;

    /// <summary>
    /// Initializes a new repository instance.
    /// </summary>
    /// <param name="context">Database context.</param>
    public EfRepository(DbContext context)
    {
        _context = context;
        _set = context.Set<TEntity>();
    }

    /// <inheritdoc />
    public Task<TEntity?> GetByIdAsync(object id, CancellationToken ct = default)
        => _set.FindAsync([id], ct).AsTask();

    /// <inheritdoc />
    public Task AddAsync(TEntity entity, CancellationToken ct = default)
        => _set.AddAsync(entity, ct).AsTask();

    /// <inheritdoc />
    public void Remove(TEntity entity) => _set.Remove(entity);
}
