using ExampleData;
using ExampleData.Infrastructure;
using ExampleLib.Infrastructure;
using ExampleLib.Domain;
using Mongo2Go;
using MongoDB.Driver;
using Xunit;

namespace ExampleLib.Tests;

public class MongoRepositoryTests : IDisposable
{
    private readonly MongoDbRunner _runner;
    private readonly IMongoDatabase _database;
    private readonly MongoGenericRepository<YourEntity> _repo;

    public MongoRepositoryTests()
    {
        _runner = MongoDbRunner.Start();
        var client = new MongoClient(_runner.ConnectionString);
        _database = client.GetDatabase("repo-tests");
        var store = new DataInMemoryValidationPlanProvider();
        store.AddPlan(new ValidationPlan<YourEntity>(e => e.Id, ThresholdType.RawDifference, 0));
        var uow = new MongoUnitOfWork(_database, new MongoValidationService(_database), store);
        _repo = new MongoGenericRepository<YourEntity>(_database, uow);
    }

    private async Task ResetAsync()
    {
        await _database.DropCollectionAsync(nameof(YourEntity));
    }

    [Fact(Skip="Requires MongoDB server")]
    public async Task AddAndCount_Works()
    {
        await ResetAsync();
        await _repo.AddAsync(new YourEntity { Name = "One", Validated = true });
        Assert.Equal(1, await _repo.CountAsync());
    }

    [Fact(Skip="Requires MongoDB server")]
    public async Task AddMany_Works()
    {
        await ResetAsync();
        await _repo.AddManyAsync(new[]
        {
            new YourEntity { Name = "One", Validated = true },
            new YourEntity { Name = "Two", Validated = true }
        });
        Assert.Equal(2, await _repo.CountAsync());
    }

    [Fact(Skip="Requires MongoDB server")]
    public async Task Delete_UnvalidatesEntity()
    {
        await ResetAsync();
        var entity = new YourEntity { Name = "Two", Validated = true };
        await _repo.AddAsync(entity);
        await _repo.DeleteAsync(entity);
        var result = await _repo.GetByIdAsync(entity.Id, includeDeleted: true);
        Assert.False(result!.Validated);
    }

    [Fact(Skip="Requires MongoDB server")]
    public async Task HardDelete_RemovesEntity()
    {
        await ResetAsync();
        var entity = new YourEntity { Name = "Three", Validated = true };
        await _repo.AddAsync(entity);
        await _repo.DeleteAsync(entity, hardDelete: true);
        Assert.Equal(0, await _repo.CountAsync());
    }

    [Fact(Skip="Requires MongoDB server")]
    public async Task GetById_CanIncludeDeleted()
    {
        await ResetAsync();
        var entity = new YourEntity { Name = "Four", Validated = true };
        await _repo.AddAsync(entity);
        await _repo.DeleteAsync(entity);
        var result = await _repo.GetByIdAsync(entity.Id, includeDeleted: true);
        Assert.NotNull(result);
    }

    public void Dispose() => _runner.Dispose();
}
