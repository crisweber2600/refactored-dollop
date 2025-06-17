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
[Scope(Feature = "WorkerRegistration")]
public class WorkerRegistrationSteps
{
    private IServiceProvider _provider = null!;

    [When("the pipeline is added with default options")]
    public void WhenAddedDefaultOptions()
    {
        var services = new ServiceCollection();
        services.AddMetricsPipeline(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        _provider = services.BuildServiceProvider();
    }

    [When("the pipeline is added with worker enabled")]
    public void WhenAddedWithWorker()
    {
        var services = new ServiceCollection();
        services.AddMetricsPipeline(
            o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()),
            opts => opts.AddWorker = true);
        _provider = services.BuildServiceProvider();
    }

    [Then("the service provider should not contain PipelineWorker")]
    public void ThenDoesNotContainWorker()
    {
        var hosted = _provider.GetServices<IHostedService>();
        hosted.Any(h => h is PipelineWorker).Should().BeFalse();
    }

    [Then("the service provider should contain PipelineWorker")]
    public void ThenContainsWorker()
    {
        var hosted = _provider.GetServices<IHostedService>();
        hosted.Any(h => h is PipelineWorker).Should().BeTrue();
    }
}
