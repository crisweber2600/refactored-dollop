using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MetricsPipeline.Core;
using Reqnroll;

namespace MetricsPipeline.Tests.Steps;

[Binding]
[Scope(Feature = "InvalidGatherMethodName")]
public class InvalidGatherMethodSteps
{
    private readonly IPipelineOrchestrator _orchestrator;
    private PipelineResult<PipelineState>? _result;

    public InvalidGatherMethodSteps(IPipelineOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [When("the orchestrator executes with an invalid gather method")]
    public async Task WhenOrchestratorExecutesInvalid()
    {
        _result = await _orchestrator.ExecuteAsync(
            "default",
            new Uri("https://api.example.com/data"),
            SummaryStrategy.Average,
            5.0,
            CancellationToken.None,
            "NonExistentMethod");
    }

    [Then("the orchestrator should return an InvalidGatherMethod error")]
    public void ThenInvalidGatherMethodError()
    {
        _result!.IsSuccess.Should().BeFalse();
        _result.Error.Should().Be("InvalidGatherMethod");
    }

    [Then("no summary should be computed or committed")]
    public void ThenNoSummary()
    {
        _result!.IsSuccess.Should().BeFalse();
    }
}
