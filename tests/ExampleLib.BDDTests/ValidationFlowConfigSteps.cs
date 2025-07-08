using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Xunit;

namespace ExampleLib.BDDTests;

[Binding]
public class ValidationFlowConfigSteps
{
    private string? _json;
    private ServiceProvider? _provider;

    [Given("the validation flow configuration")]
    public void GivenConfiguration(string multilineText)
    {
        _json = multilineText;
    }

    [When("the flows are applied")]
    public void WhenApplied()
    {
        var opts = ValidationFlowOptions.Load(_json!);
        var services = new ServiceCollection();
        services.AddValidationFlows(opts);
        _provider = services.BuildServiceProvider();
    }

    [Then("a repository for YourEntity is available")]
    public void ThenRepositoryAvailable()
    {
        Assert.NotNull(_provider!.GetService<IEntityRepository<YourEntity>>());
    }
}
