using MetricsPipeline.Core;
using MetricsPipeline.Infrastructure;
using Reqnroll;
using FluentAssertions;

namespace MetricsPipeline.Tests.Steps;

[Binding]
[Scope(Feature = "TaskExecutionStrategy")]
public class TaskStrategySteps
{
    private TaskStage _stage;
    private PipelineResult<string>? _result;

    [Given("a task stage \"(.*)\"")]
    public void GivenTaskStage(string stage)
    {
        _stage = Enum.Parse<TaskStage>(stage, true);
    }

    [When("the task executor runs")]
    [When("the task executor runs the operation")]
    public void WhenTaskExecutorRuns()
    {
        var strategy = TaskStrategyFactory.Create(_stage);
        _result = strategy.Execute();
    }

    [Then("the result message should be \"(.*)\"")]
    [Then("the result should be \"(.*)\"")]
    public void ThenResultShouldBe(string message)
    {
        _result!.Value.Should().Be(message);
    }
}
