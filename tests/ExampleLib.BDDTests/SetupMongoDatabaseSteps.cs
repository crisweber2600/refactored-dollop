using ExampleData;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Xunit;

namespace ExampleLib.BDDTests;

[Binding]
public class SetupMongoDatabaseSteps
{
    private ServiceCollection? _services;
    private ServiceProvider? _provider;

    [Given("a new service collection")]
    public void GivenNewServiceCollection()
    {
        _services = new ServiceCollection();
    }

    [When("SetupMongoDatabase is invoked")]
    public void WhenSetupMongoDatabaseInvoked()
    {
        _services!.SetupMongoDatabase("mongodb://localhost:27017", "bdd");
        _provider = _services!.BuildServiceProvider();
    }

    [Then("a Mongo unit of work can be resolved")]
    public void ThenMongoUnitOfWorkResolvable()
    {
        Assert.IsType<MongoUnitOfWork>(_provider!.GetRequiredService<IUnitOfWork>());
    }
}
