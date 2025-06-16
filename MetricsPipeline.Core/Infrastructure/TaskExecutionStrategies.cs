namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;

/// <summary>
/// Strategy used during the gather stage.
/// </summary>
public class GatherStrategy : ITaskExecutionStrategy
{
    /// <inheritdoc />
    public PipelineResult<string> Execute() => PipelineResult<string>.Success("Gathered");
}

/// <summary>
/// Strategy used during the validation stage.
/// </summary>
public class ValidateStrategy : ITaskExecutionStrategy
{
    /// <inheritdoc />
    public PipelineResult<string> Execute() => PipelineResult<string>.Success("Validated");
}

/// <summary>
/// Strategy used during the commit stage.
/// </summary>
public class CommitStrategy : ITaskExecutionStrategy
{
    /// <inheritdoc />
    public PipelineResult<string> Execute() => PipelineResult<string>.Success("Committed");
}

/// <summary>
/// Strategy used during the revert stage.
/// </summary>
public class RevertStrategy : ITaskExecutionStrategy
{
    /// <inheritdoc />
    public PipelineResult<string> Execute() => PipelineResult<string>.Success("Reverted");
}

/// <summary>
/// Factory for creating execution strategies.
/// </summary>
public static class TaskStrategyFactory
{
    /// <summary>
    /// Creates a strategy implementation for the provided stage.
    /// </summary>
    public static ITaskExecutionStrategy Create(TaskStage stage) => stage switch
    {
        TaskStage.Gather => new GatherStrategy(),
        TaskStage.Validate => new ValidateStrategy(),
        TaskStage.Commit => new CommitStrategy(),
        TaskStage.Revert => new RevertStrategy(),
        _ => throw new ArgumentOutOfRangeException(nameof(stage))
    };
}
