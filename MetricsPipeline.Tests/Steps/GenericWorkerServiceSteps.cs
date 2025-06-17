using Reqnroll;
using MetricsPipeline.Core;
using MetricsPipeline.Infrastructure;
using FluentAssertions;
using System.Linq;

namespace MetricsPipeline.Tests.Steps;

internal class WorkerDto { public double Amount { get; set; } }

[Binding]
[Scope(Feature = "GenericWorkerService")]
public class GenericWorkerServiceSteps
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly ListGatherService _worker;
    private PipelineResult<PipelineState<WorkerDto>>? _result;

    public GenericWorkerServiceSteps(IPipelineOrchestrator orchestrator, IWorkerService worker)
    {
        _orchestrator = orchestrator;
        _worker = (ListGatherService)worker;
    }

    [Given("the gather service returns:")]
    public void GivenApiReturns(string endpoint, Table table)
    {
        var values = table.Rows.Select(r => double.Parse(r[0])).ToArray();
        _worker.Metrics = values;
    }

    [When("the generic pipeline is executed selecting Amount")]
    public async Task WhenExecuted()
    {
        _result = await _orchestrator.ExecuteAsync<WorkerDto>("dto", x => x.Amount, SummaryStrategy.Sum, 100);
    }

    [Then("the summary should be (.*)")]
    public void ThenSummary(double expected)
    {
        _result!.Value.Summary.Should().Be(expected);
    }
}
