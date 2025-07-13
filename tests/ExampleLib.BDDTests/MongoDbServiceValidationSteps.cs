using ExampleData;
using ExampleData.Infrastructure;
using ExampleLib.Domain;
using Mongo2Go;
using MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace ExampleLib.BDDTests;

[Binding]
public class MongoDbServiceValidationSteps
{
    private MongoDbRunner? _runner;
    private ServiceProvider? _provider;

    [Given("a validating mongo service")]
    public void GivenService()
    {
        _runner = MongoDbRunner.Start();
        var services = new ServiceCollection();
        services.AddSingleton<IMongoDbService>(new MongoDbService(_runner.ConnectionString, "decoratorbdd"));
        services.AddSingleton(new MongoClient(_runner.ConnectionString));
        services.AddSingleton<IMongoDatabase>(sp => sp.GetRequiredService<MongoClient>().GetDatabase("decoratorbdd"));
        services.AddScoped<IValidationService, MongoValidationService>();
        services.AddScoped<IUnitOfWork, MongoUnitOfWork>();
        services.AddSingleton<IValidationPlanProvider>(sp =>
        {
            var store = new DataInMemoryValidationPlanProvider();
            store.AddPlan(new ValidationPlan<YourEntity>(e => e.Id, ThresholdType.RawDifference, 0));
            return store;
        });
        services.AddMongoDbServiceValidation();
        _provider = services.BuildServiceProvider();
    }

    [When("items are inserted via the service")]
    public async Task WhenItemsInserted()
    {
        var svc = _provider!.GetRequiredService<IMongoDbService>();
        await svc.InsertManyItemsAsync(new[] { new YourEntity { Name = "One" } }, nameof(YourEntity));
    }

    [Then("the nanny collection count should be (\\d+)")]
    public async Task ThenNannyCount(int count)
    {
        var db = _provider!.GetRequiredService<IMongoDatabase>();
        var actual = (int)await db.GetCollection<Nanny>(nameof(Nanny)).CountDocumentsAsync(Builders<Nanny>.Filter.Empty);
        if (actual != count)
            throw new Exception($"Expected {count} but was {actual}");
    }

    [AfterScenario]
    public void Cleanup()
    {
        _runner?.Dispose();
    }
}
