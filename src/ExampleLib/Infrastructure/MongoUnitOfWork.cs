using System.Linq.Expressions;
using ExampleLib.Domain;
using MongoDB.Driver;

namespace ExampleLib.Infrastructure;

public class MongoUnitOfWork : IUnitOfWork
{
    private readonly IMongoDatabase _database;
    private readonly IValidationService _validationService;
    private readonly ISummarisationPlanStore _planStore;

    public MongoUnitOfWork(IMongoDatabase database, IValidationService validationService,
        ISummarisationPlanStore planStore)
    {
        _database = database;
        _validationService = validationService;
        _planStore = planStore;
    }

    public IGenericRepository<T> Repository<T>() where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        return new MongoGenericRepository<T>(_database, _validationService);
    }

    public Task<int> SaveChangesAsync() => Task.FromResult(0);

    public Task<int> SaveChangesAsync<TEntity>(Expression<Func<TEntity, double>> selector,
        ValidationStrategy strategy,
        double threshold,
        CancellationToken cancellationToken = default)
        where TEntity : class, IValidatable, IBaseEntity, IRootEntity
    {
        return SaveChangesWithPlanAsync<TEntity>(cancellationToken);
    }

    public Task<int> SaveChangesWithPlanAsync<TEntity>(CancellationToken cancellationToken = default)
        where TEntity : class, IValidatable, IBaseEntity, IRootEntity
    {
        return Task.FromResult(0);
    }
}
