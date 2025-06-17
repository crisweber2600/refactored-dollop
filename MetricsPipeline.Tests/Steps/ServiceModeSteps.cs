using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MetricsPipeline.Core;
using MetricsPipeline.Infrastructure;
using Reqnroll;

namespace MetricsPipeline.Tests.Steps;

[Binding]
[Scope(Feature = "ServiceModeSelection")]
public class ServiceModeSteps
{
    private IServiceProvider? _provider;

    [When("the service provider is built with mode (.*)")]
    public void WhenProviderBuiltWithMode(string mode)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddMetricsPipeline(o => o.UseInMemoryDatabase("demo"), Enum.Parse<PipelineMode>(mode));
                services.AddHttpClient<HttpMetricsClient>();
            })
            .Build();
        _provider = host.Services;
    }

    [When("the service provider is built with default mode")]
    public void WhenProviderBuiltDefault()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddMetricsPipeline(o => o.UseInMemoryDatabase("demo"));
            })
            .Build();
        _provider = host.Services;
    }

    [Then("IGatherService should resolve to (.*)")]
    public void ThenGatherService(string type)
    {
        var svc = _provider!.GetRequiredService<IGatherService>();
        if (type == nameof(HttpGatherService))
            svc.Should().BeOfType<HttpGatherService>();
        else
            svc.Should().BeOfType<InMemoryGatherService>();
    }

    [Then("IWorkerService should resolve to (.*)")]
    public void ThenWorkerService(string type)
    {
        var svc = _provider!.GetRequiredService<IWorkerService>();
        if (type == nameof(HttpWorkerService))
            svc.Should().BeOfType<HttpWorkerService>();
        else
            svc.Should().BeOfType<InMemoryGatherService>();
    }
}
