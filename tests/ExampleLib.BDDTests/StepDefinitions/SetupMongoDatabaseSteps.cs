using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Xunit;

namespace ExampleLib.BDDTests;

[Binding]
public class SetupMongoDatabaseSteps
{
    private ServiceCollection? _services;
    private ServiceProvider? _provider;

    [Given("a new mongo service collection")]
    public void GivenNewMongoServiceCollection()
    {
        _services = new ServiceCollection();
    }

    [When("SetupMongoDatabase is invoked")]
    public void WhenSetupMongoDatabaseInvoked()
    {
        ExampleData.ServiceCollectionExtensions.SetupMongoDatabase(
            _services!, "mongodb://localhost:27017", "bdd");
        _services!.AddSingleton<ISaveAuditRepository, InMemorySaveAuditRepository>();
        _provider = _services!.BuildServiceProvider();
    }

    [Then("a Mongo unit of work can be resolved")]
    public void ThenMongoUnitOfWorkResolvable()
    {
        Assert.IsType<MongoUnitOfWork>(_provider!.GetRequiredService<IUnitOfWork>());
    }
}
