using ExampleData;
using MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Xunit;

namespace ExampleLib.BDDTests;

[Binding]
public class MongoConfigurationSteps
{
    private ServiceCollection? _services;
    private ServiceProvider? _provider;

    [Given("a new service collection")]
    public void GivenNewServiceCollection()
    {
        _services = new ServiceCollection();
    }

    [When("AddExampleDataMongo is invoked")]
    public void WhenAddExampleDataMongoInvoked()
    {
        _services!.AddExampleDataMongo("mongodb://localhost:27017", "bdd");
        _provider = _services.BuildServiceProvider();
    }

    [Then("the Mongo database should resolve")]
    public void ThenMongoDatabaseResolvable()
    {
        Assert.NotNull(_provider!.GetService<IMongoDatabase>());
    }
}
