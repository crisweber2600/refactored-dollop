using ExampleData;
using ExampleData.Infrastructure;
using ExampleLib.Domain;
using Mongo2Go;
using MongoDB.Driver;
using Xunit;

namespace ExampleLib.Tests;

public class MongoDbServiceValidationTests : IDisposable
{
    private readonly MongoDbRunner _runner;
    private readonly IMongoDatabase _database;
    private readonly IMongoDbService _service;

    public MongoDbServiceValidationTests()
    {
        _runner = MongoDbRunner.Start();
        var client = new MongoClient(_runner.ConnectionString);
        _database = client.GetDatabase("decoratortests");
        var store = new DataInMemoryValidationPlanProvider();
        store.AddPlan(new ValidationPlan<YourEntity>(e => e.Id, ThresholdType.RawDifference, 0));
        var uow = new MongoUnitOfWork(_database, new MongoValidationService(_database), store);
        var inner = new MongoDbService(_runner.ConnectionString, "decoratortests");
        _service = new MongoDbServiceValidationDecorator(inner, uow);
    }

    [Fact(Skip="Requires MongoDB server")]
    public async Task InsertMany_WritesNanny()
    {
        await _service.InsertManyItemsAsync(new[] { new YourEntity { Name = "One" } }, nameof(YourEntity));
        var count = await _database.GetCollection<Nanny>(nameof(Nanny)).CountDocumentsAsync(Builders<Nanny>.Filter.Empty);
        Assert.Equal(1, count);
    }

    public void Dispose() => _runner.Dispose();
}
