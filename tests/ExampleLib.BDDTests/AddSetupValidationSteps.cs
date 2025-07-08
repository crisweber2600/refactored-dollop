using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Xunit;

namespace ExampleLib.BDDTests;

[Binding]
public class AddSetupValidationSteps
{
    private ServiceCollection? _services;
    private ServiceProvider? _provider;

    [Given("a new service collection")]
    public void GivenNewServiceCollection()
    {
        _services = new ServiceCollection();
    }

    [When("AddSetupValidation is invoked")]
    public void WhenInvoked()
    {
        _services!.AddSetupValidation<YourEntity>(
            b => b.UseSqlServer<YourDbContext>("DataSource=:memory:"),
            e => e.Id);
        _provider = _services.BuildServiceProvider();
    }

    [When("AddSetupValidation is invoked with Mongo")]
    public void WhenInvokedMongo()
    {
        _services!.AddSetupValidation<YourEntity>(
            b => b.UseMongo("mongodb://localhost:27017", "bdd"),
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
