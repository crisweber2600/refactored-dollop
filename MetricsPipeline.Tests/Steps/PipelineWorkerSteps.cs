using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MetricsPipeline.Core;
using MetricsPipeline.Infrastructure;
using Reqnroll;

namespace MetricsPipeline.Tests.Steps;

internal record MetricDto { public double Value { get; set; } }

// Simple orchestrator used to control success or failure in tests
internal class FakeOrchestrator : IPipelineOrchestrator
{
    private readonly bool _success;
    public FakeOrchestrator(bool success) { _success = success; }

    public Task<PipelineResult<PipelineState<T>>> ExecuteAsync<T>(string name, Func<T, double> selector, SummaryStrategy strategy, double threshold, CancellationToken ct = default, string workerMethod = "FetchAsync")
    {
        if (_success)
        {
            var state = new PipelineState<T>(name, new Uri("/", UriKind.Relative), Array.Empty<T>(), 0, 0, threshold, DateTime.UtcNow);
            return Task.FromResult(PipelineResult<PipelineState<T>>.Success(state));
        }
        return Task.FromResult(PipelineResult<PipelineState<T>>.Failure("fail"));
    }
}

internal class TestWorker : PipelineWorker
{
    public TestWorker(IPipelineOrchestrator orchestrator)
        : base(orchestrator) { }
    public Task RunAsync() => base.ExecuteAsync(CancellationToken.None);
}

[Binding]
[Scope(Feature = "WorkerStageExecution")]
public class PipelineWorkerSteps
{
    private TestWorker _worker = null!;

    [Given("the worker is configured for (.*)")]
    public void GivenWorkerConfigured(string outcome)
    {
        bool success = outcome == "success";
        _worker = new TestWorker(new FakeOrchestrator(success));
    }

    [When("the worker runs")]
    public async Task WhenWorkerRuns()
    {
        await _worker.RunAsync();
    }

    [Then("the stage results should be \"(.*)\"")]
    public void ThenResultsShouldBe(string expected)
    {
        var parts = expected.Split(',');
        _worker.ExecutedStages.Should().Equal(parts);
    }
}
