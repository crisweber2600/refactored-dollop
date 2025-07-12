using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Xunit;

namespace ExampleLib.BDDTests;

[Binding]
public class AddValidationSteps
{
    private ServiceCollection? _services;
    private ServiceProvider? _provider;

    [Given("a new service collection")]
    public void GivenNewServiceCollection()
    {
        _services = new ServiceCollection();
    }

    [When("AddValidationForEfCore is invoked")]
    public void WhenInvoked()
    {
        _services!.AddValidationForEfCore<YourEntity, YourDbContext>(
            "DataSource=:memory:",
            e => e.Id);
        _provider = _services.BuildServiceProvider();
    }

    [When("AddValidationForMongo is invoked")]
    public void WhenInvokedMongo()
    {
        _services!.AddValidationForMongo<YourEntity>(
            "mongodb://localhost:27017",
            "bdd",
            e => e.Id);
        _provider = _services.BuildServiceProvider();
    }

    [Then("a repository and validator can be resolved")]
    public void ThenServicesResolvable()
    {
        Assert.NotNull(_provider!.GetService<IEntityRepository<YourEntity>>());
        Assert.NotNull(_provider!.GetService<ISummarisationValidator<YourEntity>>());
    }

    [Then("a mongo repository and validator can be resolved")]
    public void ThenMongoRepositoryResolvable()
    {
        Assert.IsType<MongoSaveAuditRepository>(_provider!.GetService<ISaveAuditRepository>());
        Assert.NotNull(_provider!.GetService<ISummarisationValidator<YourEntity>>());
    }
}
