using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Xunit;

namespace ExampleLib.BDDTests;

[Binding]
public class SetupDatabaseSteps
{
    private ServiceCollection? _services;
    private ServiceProvider? _provider;

    [Given("a new service collection")]
    public void GivenNewServiceCollection()
    {
        _services = new ServiceCollection();
    }

    [When("SetupDatabase is invoked")]
    public void WhenSetupDatabaseInvoked()
    {
        ExampleData.ServiceCollectionExtensions.SetupDatabase<YourDbContext>(_services!, "DataSource=:memory:");
        _services!.AddSingleton<ISaveAuditRepository, InMemorySaveAuditRepository>();
        _provider = _services!.BuildServiceProvider();
    }

    [Then("a unit of work can be resolved")]
    public void ThenUnitOfWorkResolvable()
    {
        Assert.NotNull(_provider!.GetService<IUnitOfWork>());
    }
}
