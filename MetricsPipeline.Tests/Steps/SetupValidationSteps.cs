using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MetricsPipeline.Infrastructure;
using Reqnroll;

namespace MetricsPipeline.Tests.Steps;

[Binding]
[Scope(Feature = "SetupValidation")]
public class SetupValidationSteps
{
    private IServiceProvider _provider = null!;

    [When("the setup validation is executed")]
    public void WhenSetupExecuted()
    {
        var services = new ServiceCollection();
        services.SetupValidation(o =>
        {
            o.ConfigureDb = b => b.UseInMemoryDatabase(Guid.NewGuid().ToString());
            o.ConfigureClient = (_, c) => c.BaseAddress = new Uri("https://example.com/");
        });
        _provider = services.BuildServiceProvider();
    }

    [When("a typed validator is registered")]
    public void WhenTypedValidatorRegistered()
    {
        var services = new ServiceCollection();
        services.SetupValidation(o => o.ConfigureDb = b => b.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddSetupValidation<DemoValidator>();
        _provider = services.BuildServiceProvider();
    }

    [Then("the service provider should resolve SummaryDbContext")]
    public void ThenContextResolved()
    {
        _provider.GetService<SummaryDbContext>().Should().NotBeNull();
    }

    [Then("the service provider should resolve HttpClient")]
    public void ThenClientResolved()
    {
        var factory = _provider.GetService<IHttpClientFactory>();
        factory.Should().NotBeNull();
        factory!.CreateClient("validation").Should().NotBeNull();
    }

    [Then("the typed validator should resolve")]
    public void ThenValidatorResolved()
    {
        _provider.GetService<DemoValidator>().Should().NotBeNull();
    }

    internal class DemoValidator : SetupValidator
    {
        public DemoValidator(SetupValidationOptions options) : base(options) { }
    }
}
