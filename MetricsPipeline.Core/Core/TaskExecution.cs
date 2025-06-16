namespace MetricsPipeline.Core;

/// <summary>
/// Pipeline stages supported by the execution strategy framework.
/// </summary>
public enum TaskStage
{
    /// <summary>Gather metrics.</summary>
    Gather,
    /// <summary>Validate the summarized value.</summary>
    Validate,
    /// <summary>Commit a valid summary.</summary>
    Commit,
    /// <summary>Revert when validation fails.</summary>
    Revert
}

/// <summary>
/// Strategy executed for a given pipeline stage.
/// </summary>
public interface ITaskExecutionStrategy
{
    /// <summary>
    /// Execute the behaviour for a particular pipeline stage.
    /// </summary>
    /// <returns>A message describing the outcome.</returns>
    PipelineResult<string> Execute();
}
