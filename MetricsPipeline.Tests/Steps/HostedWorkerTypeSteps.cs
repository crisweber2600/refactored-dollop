using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MetricsPipeline.ConsoleApp;
using MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;
using Reqnroll;

namespace MetricsPipeline.Tests.Steps;

[Binding]
[Scope(Feature = "HostedWorkerType")]
public class HostedWorkerTypeSteps
{
    private GenericMetricsWorker _worker = null!;
    private IReadOnlyList<GenericMetricsWorker.MetricDto> _items = null!;

    [When("the pipeline is added with demo worker type")]
    public void WhenAddedWithWorkerType()
    {
        var services = new ServiceCollection();
        services.AddMetricsPipeline(
            typeof(GenericMetricsWorker),
            o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()),
            opts => opts.AddWorker = true);
        var provider = services.BuildServiceProvider();
        _worker = (GenericMetricsWorker)provider.GetServices<IHostedService>().Single(h => h is GenericMetricsWorker);
    }

    [When("the demo worker runs")]
    public async Task WhenWorkerRuns()
    {
        _items = await _worker.RunAsync(CancellationToken.None);
    }

    [Then("the demo worker should return (\\d+) items")]
    public void ThenShouldReturn(int expected)
    {
        _items.Count.Should().Be(expected);
    }
}
