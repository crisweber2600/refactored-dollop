using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Linq;
using ExampleLib.Domain;

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
    private readonly IBatchValidationService _batchValidator;

    public UnitOfWork(TContext context, IValidationService validationService,
        ExampleLib.Domain.ISummarisationPlanStore planStore,
        IBatchValidationService batchValidator)
    {
        _context = context;
        _validationService = validationService;
        _planStore = planStore;
        _batchValidator = batchValidator;
    }

    public IGenericRepository<T> Repository<T>() where T : class, IValidatable, IBaseEntity, IRootEntity => new EfGenericRepository<T>((YourDbContext)(DbContext)_context, _batchValidator);

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
            var isValid = await _validationService.ValidateAndSaveAsync(entry.Entity, entry.Entity.Id.ToString(), cancellationToken);
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
            var isValid = await _validationService.ValidateAndSaveAsync(entry.Entity, entry.Entity.Id.ToString(), cancellationToken);
            entry.Entity.Validated = isValid;
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }
}
