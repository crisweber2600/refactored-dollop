namespace MetricsPipeline.Core;

/// <summary>
/// Pipeline stages supported by the execution strategy framework.
/// </summary>
public enum TaskStage
{
    Gather,
    Validate,
    Commit,
    Revert
}

public interface ITaskExecutionStrategy
{
    /// <summary>
    /// Execute the behaviour for a particular pipeline stage.
    /// </summary>
    PipelineResult<string> Execute();
}
