using ExampleLib.Domain;
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
    public void WhenAddValidatorServiceInvoked()
    {
        _services!.AddValidatorService();
        _provider = _services.BuildServiceProvider();
    }

    [When("AddValidatorRule is invoked")]
    public void WhenAddValidatorRuleInvoked()
    {
        _services!.AddValidatorRule<object>(_ => true);
        _provider = _services.BuildServiceProvider();
    }

    [Then("the manual validator can be resolved")]
    public void ThenManualValidatorResolvable()
    {
        Assert.NotNull(_provider!.GetService<IManualValidatorService>());
    }

    [Then("the manual validator validates successfully")]
    public void ThenManualValidatorValidates()
    {
        var service = _provider!.GetRequiredService<IManualValidatorService>();
        Assert.True(service.Validate(new object()));
    }
}
