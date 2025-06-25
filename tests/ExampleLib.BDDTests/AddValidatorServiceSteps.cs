using ExampleLib.Domain;
using ExampleData;
using ExampleLib.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Xunit;

namespace ExampleLib.BDDTests;

[Binding]
public class AddValidatorServiceSteps
{
    private ServiceCollection? _services;
    private ServiceProvider? _provider;

    [Given("a new service collection")]
    public void GivenNewServiceCollection()
    {
        _services = new ServiceCollection();
    }

    [When("AddValidatorService is invoked")]
    public void WhenAddValidatorService()
    {
        _services!.AddValidatorService();
    }

    [When("AddValidatorRule is added")]
    public void WhenAddValidatorRule()
    {
        _services!.AddValidatorRule<YourEntity>(_ => true);
        _provider = _services.BuildServiceProvider();
    }

    [Then("a manual validator can be resolved")]
    public void ThenManualValidatorResolvable()
    {
        Assert.NotNull(_provider!.GetService<IManualValidatorService>());
    }
}
