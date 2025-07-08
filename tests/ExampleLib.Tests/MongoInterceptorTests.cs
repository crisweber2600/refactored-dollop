using ExampleData;
using ExampleData.Infrastructure;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Mongo2Go;
using MongoDB.Driver;
using Xunit;

namespace ExampleLib.Tests;

public class MongoInterceptorTests : IDisposable
{
    private readonly MongoDbRunner _runner;
    private readonly IMongoDatabase _database;
    private readonly MongoUnitOfWork _uow;
    private readonly IGenericRepository<YourEntity> _repo;

    public MongoInterceptorTests()
    {
        _runner = MongoDbRunner.Start();
        var client = new MongoClient(_runner.ConnectionString);
        _database = client.GetDatabase("interceptor-tests");
        var store = new InMemorySummarisationPlanStore();
        store.AddPlan(new SummarisationPlan<YourEntity>(e => e.Id, ThresholdType.RawDifference, 0));
        _uow = new MongoUnitOfWork(_database, new MongoValidationService(_database), store);
        _repo = _uow.Repository<YourEntity>();
    }

    [Fact(Skip="Requires MongoDB server")]
    public async Task AddAsync_TriggersSaveChanges()
    {
        await _repo.AddAsync(new YourEntity { Name = "Added" });

        var nanny = await _database.GetCollection<Nanny>(nameof(Nanny))
            .Find(Builders<Nanny>.Filter.Empty)
            .FirstOrDefaultAsync();

        Assert.NotNull(nanny);
    }

    [Fact(Skip="Requires MongoDB server")]
    public async Task UpdateAsync_TriggersSaveChanges()
    {
        var entity = new YourEntity { Name = "Delete" };
        await _repo.AddAsync(entity);

        await _repo.DeleteAsync(entity);

        var nanny = await _database.GetCollection<Nanny>(nameof(Nanny))
            .Find(Builders<Nanny>.Filter.Empty)
            .FirstOrDefaultAsync();

        Assert.NotNull(nanny);
    }

    public void Dispose() => _runner.Dispose();
}
