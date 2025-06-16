using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MetricsPipeline.Core;
using MetricsPipeline.Infrastructure;
using Reqnroll;

namespace MetricsPipeline.Tests.Steps;

// Simple orchestrator used to control success or failure in tests
internal class FakeOrchestrator : IPipelineOrchestrator
{
    private readonly bool _success;
    public FakeOrchestrator(bool success) { _success = success; }

    public Task<PipelineResult<PipelineState>> ExecuteAsync(string name, Uri source, SummaryStrategy strategy, double threshold, CancellationToken ct = default, string gatherMethodName = "FetchMetricsAsync")
    {
        if (_success)
        {
            var state = new PipelineState(name, source, Array.Empty<double>(), 0, 0, threshold, DateTime.UtcNow);
            return Task.FromResult(PipelineResult<PipelineState>.Success(state));
        }
        return Task.FromResult(PipelineResult<PipelineState>.Failure("fail"));
    }
}

// Exposes the protected ExecuteAsync method for testing
internal class FakeGatherService : IGatherService
{
    public Task<PipelineResult<IReadOnlyList<double>>> FetchMetricsAsync(Uri source, CancellationToken ct = default)
        => Task.FromResult(PipelineResult<IReadOnlyList<double>>.Success(new List<double>{1.0,2.0}));
}

internal class TestWorker : PipelineWorker
{
    public TestWorker(IPipelineOrchestrator orchestrator)
        : base(orchestrator, new FakeGatherService()) { }
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
