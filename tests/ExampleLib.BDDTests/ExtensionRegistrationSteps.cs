using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Xunit;

namespace ExampleLib.BDDTests;

[Binding]
public class ExtensionRegistrationSteps
{
    private ServiceCollection? _services;
    private ServiceProvider? _provider;

    [Given("a new service collection")]
    public void GivenNewServiceCollection()
    {
        _services = new ServiceCollection();
    }

    [When("AddSaveValidation is invoked")]
    public void WhenInvoked()
    {
        _services!.AddSaveValidation<YourEntity>(e => e.Id);
        _provider = _services.BuildServiceProvider();
    }

    [Then("a repository and validator can be resolved")]
    public void ThenServicesResolvable()
    {
        Assert.NotNull(_provider!.GetService<IEntityRepository<YourEntity>>());
        Assert.NotNull(_provider.GetService<ISummarisationValidator<YourEntity>>());
    }
}
