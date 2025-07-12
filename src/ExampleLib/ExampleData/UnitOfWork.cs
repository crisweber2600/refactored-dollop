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

    /// <summary>
    /// Validate and save changes using the summarisation plan registered for
    /// <typeparamref name="TEntity"/>.
    /// </summary>
    Task<int> SaveChangesWithPlanAsync<TEntity>(CancellationToken cancellationToken = default)
        where TEntity : class, IValidatable, IBaseEntity, IRootEntity;
}

public class UnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    private readonly TContext _context;
    private readonly IValidationService _validationService;
    private readonly ExampleLib.Domain.ISummarisationPlanStore _planStore;

    public UnitOfWork(TContext context, IValidationService validationService,
        ExampleLib.Domain.ISummarisationPlanStore planStore)
    {
        _context = context;
        _validationService = validationService;
        _planStore = planStore;
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

    /// <summary>
    /// Validate and save changes using a set of rules. All rules must be satisfied
    /// for the entity to be marked as validated.
    /// </summary>
    public async Task<int> SaveChangesAsync<TEntity>(ValidationRuleSet<TEntity> ruleSet,
        CancellationToken cancellationToken = default)
        where TEntity : class, IValidatable, IBaseEntity, IRootEntity
    {
        if (ruleSet == null) throw new ArgumentNullException(nameof(ruleSet));

        double summary = 0;
        var isValid = true;

        foreach (var rule in ruleSet.Rules)
        {
            summary = await _validationService.ComputeAsync(ruleSet.Selector, rule.Strategy, cancellationToken);
            if (summary < rule.Threshold)
            {
                isValid = false;
            }
        }

        foreach (var entry in _context.ChangeTracker.Entries<TEntity>()
                     .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
        {
            entry.Entity.Validated = isValid;
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

    public Task<int> SaveChangesWithPlanAsync<TEntity>(CancellationToken cancellationToken = default)
        where TEntity : class, IValidatable, IBaseEntity, IRootEntity
    {
        var plan = _planStore.GetPlan<TEntity>();
        Expression<Func<TEntity, double>> selector = e => (double)plan.MetricSelector(e);
        return SaveChangesAsync(selector, ValidationStrategy.Sum, (double)plan.ThresholdValue, cancellationToken);
    }
}
