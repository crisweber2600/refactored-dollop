using System.Linq.Expressions;
using MongoDB.Driver;
using ExampleLib.Domain;

namespace ExampleData;

public class MongoUnitOfWork : IUnitOfWork
{
    private readonly IMongoDatabase _database;
    private readonly IValidationService _validationService;
    private readonly ExampleLib.Domain.ISummarisationPlanStore _planStore;
    private readonly IBatchValidationService _batchValidator;

    public MongoUnitOfWork(IMongoDatabase database, IValidationService validationService,
        ExampleLib.Domain.ISummarisationPlanStore planStore,
        IBatchValidationService batchValidator)
    {
        _database = database;
        _validationService = validationService;
        _planStore = planStore;
        _batchValidator = batchValidator;
    }

    public IGenericRepository<T> Repository<T>() where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        return new MongoGenericRepository<T>(_database, _validationService, _batchValidator);
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
