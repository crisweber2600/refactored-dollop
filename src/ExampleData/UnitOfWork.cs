using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Linq;

namespace ExampleData;

public interface IUnitOfWork
{
    IGenericRepository<T> Repository<T>() where T : class, IValidatable, IBaseEntity, IRootEntity;
    Task<int> SaveChangesAsync();
    Task<int> SaveChangesAsync<TEntity>(Expression<Func<TEntity, double>> selector,
        ValidationStrategy strategy,
        double threshold,
        CancellationToken cancellationToken = default)
        where TEntity : class, IValidatable, IBaseEntity, IRootEntity;
}

public class UnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    private readonly TContext _context;
    private readonly IValidationService _validationService;

    public UnitOfWork(TContext context, IValidationService validationService)
    {
        _context = context;
        _validationService = validationService;
    }

    public IGenericRepository<T> Repository<T>() where T : class, IValidatable, IBaseEntity, IRootEntity => new EfGenericRepository<T>((YourDbContext)(DbContext)_context);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public async Task<int> SaveChangesAsync<TEntity>(Expression<Func<TEntity, double>> selector,
        ValidationStrategy strategy,
        double threshold,
        CancellationToken cancellationToken = default)
        where TEntity : class, IValidatable, IBaseEntity, IRootEntity
    {
        var summary = await _validationService.ComputeAsync(selector, strategy, cancellationToken);

        foreach (var entry in _context.ChangeTracker.Entries<TEntity>()
                     .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
        {
            entry.Entity.Validated = summary >= threshold;
        }

        if (_context is YourDbContext db)
        {
            db.Nannies.Add(new Nanny
            {
                ProgramName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown",
                Entity = typeof(TEntity).Name,
                SummarizedValue = summary,
                DateTime = DateTime.UtcNow,
                RuntimeID = Guid.NewGuid()
            });
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }
}
