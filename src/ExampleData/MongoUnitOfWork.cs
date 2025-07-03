using System.Linq.Expressions;
using MongoDB.Driver;

namespace ExampleData;

public class MongoUnitOfWork : IUnitOfWork
{
    private readonly IMongoDatabase _database;
    private readonly IValidationService _validationService;
    private readonly ExampleLib.Domain.ISummarisationPlanStore _planStore;

    public MongoUnitOfWork(IMongoDatabase database, IValidationService validationService,
        ExampleLib.Domain.ISummarisationPlanStore planStore)
    {
        _database = database;
        _validationService = validationService;
        _planStore = planStore;
    }

    public IGenericRepository<T> Repository<T>() where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        return new MongoGenericRepository<T>(_database);
    }

    public Task<int> SaveChangesAsync() => Task.FromResult(0);

    public async Task<int> SaveChangesAsync<TEntity>(Expression<Func<TEntity, double>> selector,
        ValidationStrategy strategy,
        double threshold,
        CancellationToken cancellationToken = default)
        where TEntity : class, IValidatable, IBaseEntity, IRootEntity
    {
        var summary = await _validationService.ComputeAsync(selector, strategy, cancellationToken);
        var nannyCollection = _database.GetCollection<Nanny>(nameof(Nanny));
        await nannyCollection.InsertOneAsync(new Nanny
        {
            ProgramName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown",
            Entity = typeof(TEntity).Name,
            SummarizedValue = summary,
            DateTime = DateTime.UtcNow,
            RuntimeID = Guid.NewGuid()
        }, cancellationToken: cancellationToken);
        return 0;
    }

    public Task<int> SaveChangesWithPlanAsync<TEntity>(CancellationToken cancellationToken = default)
        where TEntity : class, IValidatable, IBaseEntity, IRootEntity
    {
        var plan = _planStore.GetPlan<TEntity>();
        Expression<Func<TEntity, double>> selector = e => (double)plan.MetricSelector(e);
        return SaveChangesAsync(selector, ValidationStrategy.Sum, (double)plan.ThresholdValue, cancellationToken);
    }
}
