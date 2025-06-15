using Reqnroll;
using MetricsPipeline.Core;
using FluentAssertions;

[Binding]
public class IntegrationSteps
{
    private readonly IPipelineOrchestrator _orchestrator;
    private PipelineResult<PipelineState>? _run;

    public IntegrationSteps(IPipelineOrchestrator orchestrator) => _orchestrator = orchestrator;

    [When(@"the pipeline is executed")]
    public async Task WhenPipelineExecuted()
    {
        var source = new Uri("https://api.example.com/data");
        _run = await _orchestrator.ExecuteAsync(source, SummaryStrategy.Average, 5.0);
    }

    [Then(@"the summary should be (.*)")]
    public void ThenSummaryShouldBe(double expected)
    {
        _run!.Value.Summary.Should().Be(expected);
    }

    [Then(@"the delta from last run should be (.*)")]
    public void ThenDelta(double delta)
    {
        var diff = Math.Abs(_run!.Value.Summary!.Value - _run!.Value.LastCommittedSummary!.Value);
        diff.Should().Be(delta);
    }

    [Then(@"the summary should be committed successfully")]
    public void ThenCommitted()
    {
        _run!.IsSuccess.Should().BeTrue();
    }

    [Then(@"the summary should not be committed")]
    public void ThenNotCommitted()
    {
        _run!.IsSuccess.Should().BeFalse();
    }
}
