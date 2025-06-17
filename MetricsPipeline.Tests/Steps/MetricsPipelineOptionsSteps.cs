using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;
using Microsoft.EntityFrameworkCore;
using Reqnroll;

namespace MetricsPipeline.Tests.Steps;

[Binding]
[Scope(Feature = "MetricsPipelineOptions")]
public class MetricsPipelineOptionsSteps
{
    private IServiceProvider _provider = null!;

    [When("the pipeline is added with hosted worker")]
    public void WhenAddedWithHostedWorker()
    {
        var services = new ServiceCollection();
        services.AddMetricsPipeline(
            o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()),
            opts => opts.AddWorker = true);
        _provider = services.BuildServiceProvider();
    }

    [When("the pipeline is added with HttpClient")]
    public void WhenAddedWithHttpClient()
    {
        var services = new ServiceCollection();
        services.AddMetricsPipeline(
            o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()),
            opts => opts.RegisterHttpClient = true);
        _provider = services.BuildServiceProvider();
    }

    [When("the pipeline is added with HTTP worker")]
    public void WhenAddedWithHttpWorker()
    {
        var services = new ServiceCollection();
        services.AddMetricsPipeline(
            o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()),
            opts => opts.WorkerMode = WorkerMode.Http);
        _provider = services.BuildServiceProvider();
    }

    [When("the pipeline is added with default worker mode")]
    public void WhenAddedDefaultWorker()
    {
        var services = new ServiceCollection();
        services.AddMetricsPipeline(
            o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        _provider = services.BuildServiceProvider();
    }

    [Then("the service provider should contain PipelineWorker")]
    public void ThenContainsWorker()
    {
        var hosted = _provider.GetServices<IHostedService>();
        hosted.Any(h => h is PipelineWorker).Should().BeTrue();
    }

    [Then("the service provider should contain HttpMetricsClient")]
    public void ThenContainsClient()
    {
        _provider.GetService<HttpMetricsClient>().Should().NotBeNull();
    }

    [Then("IGatherService should be HttpGatherService")]
    public void ThenGatherServiceHttp()
    {
        _provider.GetService<IGatherService>().Should().BeOfType<HttpGatherService>();
        _provider.GetService<IWorkerService>().Should().BeOfType<HttpWorkerService>();
    }

    [Then("IGatherService should be InMemoryGatherService")]
    public void ThenGatherServiceMemory()
    {
        _provider.GetService<IGatherService>().Should().BeOfType<InMemoryGatherService>();
        _provider.GetService<IWorkerService>().Should().BeOfType<InMemoryGatherService>();
    }
}
