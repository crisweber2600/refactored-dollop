namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Generic unit of work implementation for EF Core contexts.
/// </summary>
public class UnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    private readonly TContext _context;

    /// <summary>
    /// Initializes a new unit of work for the specified context.
    /// </summary>
    public UnitOfWork(TContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public IRepository<TEntity> Repository<TEntity>() where TEntity : class
        => new EfRepository<TEntity>(_context);

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
