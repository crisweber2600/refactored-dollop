using System.Linq.Expressions;
using ExampleLib.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExampleLib.Infrastructure;

public class UnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    private readonly TContext _context;
    private readonly IValidationService _validationService;
    private readonly ISummarisationPlanStore _planStore;

    public UnitOfWork(TContext context, IValidationService validationService,
        ISummarisationPlanStore planStore)
    {
        _context = context;
        _validationService = validationService;
        _planStore = planStore;
    }

    public IGenericRepository<T> Repository<T>() where T : class, IValidatable, IBaseEntity, IRootEntity => new EfGenericRepository<T>(_context);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public Task<int> SaveChangesAsync<TEntity>(Expression<Func<TEntity, double>> selector,
        ValidationStrategy strategy,
        double threshold,
        CancellationToken cancellationToken = default)
        where TEntity : class, IValidatable, IBaseEntity, IRootEntity
    {
        return SaveChangesWithPlanAsync<TEntity>(cancellationToken);
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

        foreach (var entry in _context.ChangeTracker.Entries<TEntity>()
                     .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
        {
            var isValid = await _validationService.ValidateAndSaveAsync(entry.Entity, cancellationToken);
            entry.Entity.Validated = isValid;
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> SaveChangesWithPlanAsync<TEntity>(CancellationToken cancellationToken = default)
        where TEntity : class, IValidatable, IBaseEntity, IRootEntity
    {
        foreach (var entry in _context.ChangeTracker.Entries<TEntity>()
                     .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
        {
            var isValid = await _validationService.ValidateAndSaveAsync(entry.Entity, cancellationToken);
            entry.Entity.Validated = isValid;
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }
}
