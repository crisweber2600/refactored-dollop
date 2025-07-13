using ExampleData;
using ExampleData.Infrastructure;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Mongo2Go;
using MongoDB.Driver;
using Reqnroll;

namespace ExampleLib.BDDTests;

[Binding]
public class MongoRepoSteps
{
    private readonly MongoDbRunner _runner;
    private readonly IMongoDatabase _database;
    private readonly MongoGenericRepository<YourEntity> _repository;
    private YourEntity? _entity;

    public MongoRepoSteps()
    {
        _runner = MongoDbRunner.Start();
        var client = new MongoClient(_runner.ConnectionString);
        _database = client.GetDatabase("bddrepo");
        var store = new ExampleData.Infrastructure.DataInMemoryValidationPlanProvider();
        store.AddPlan(new ExampleLib.ValidationPlan<YourEntity>(e => e.Id, ExampleLib.Domain.ThresholdType.RawDifference, 0));
        var uow = new MongoUnitOfWork(_database, new MongoValidationService(_database), store);
        _repository = new MongoGenericRepository<YourEntity>(_database, uow);
    }

    [Given("a clean mongo database")]
    public async Task GivenCleanDatabase()
    {
        await _database.DropCollectionAsync(nameof(YourEntity));
    }

    [When("a new mongo entity is added")]
    public async Task WhenNewEntityAdded()
    {
        await _repository.AddAsync(new YourEntity { Name = "Test", Validated = true });
    }

    [When("two mongo entities are bulk inserted")]
    public async Task WhenTwoEntitiesBulkInserted()
    {
        await _repository.AddManyAsync(new[]
        {
            new YourEntity { Name = "One", Validated = true },
            new YourEntity { Name = "Two", Validated = true }
        });
    }

    [Then("the mongo repository count should be (\\d+)")]
    public async Task ThenRepoCount(int count)
    {
        var actual = await _repository.CountAsync();
        if (actual != count)
            throw new Exception($"Expected {count} but was {actual}");
    }

    [Given("a mongo entity to delete")]
    public async Task GivenEntityToDelete()
    {
        _entity = new YourEntity { Name = "Delete", Validated = true };
        await _repository.AddAsync(_entity);
    }

    [When("the mongo entity is deleted")]
    public async Task WhenEntityDeleted()
    {
        if (_entity != null)
            await _repository.DeleteAsync(_entity);
    }

    [Then("the mongo entity should be marked unvalidated")]
    public async Task ThenEntityUnvalidated()
    {
        var result = await _repository.GetByIdAsync(_entity!.Id, includeDeleted: true);
        if (result == null || result.Validated)
            throw new Exception("Entity was not soft deleted");
    }

    [AfterScenario]
    public void Cleanup() => _runner.Dispose();
}
