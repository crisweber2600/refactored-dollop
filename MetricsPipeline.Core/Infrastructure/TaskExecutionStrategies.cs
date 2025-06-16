namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;

public class GatherStrategy : ITaskExecutionStrategy
{
    public PipelineResult<string> Execute() => PipelineResult<string>.Success("Gathered");
}

public class ValidateStrategy : ITaskExecutionStrategy
{
    public PipelineResult<string> Execute() => PipelineResult<string>.Success("Validated");
}

public class CommitStrategy : ITaskExecutionStrategy
{
    public PipelineResult<string> Execute() => PipelineResult<string>.Success("Committed");
}

public class RevertStrategy : ITaskExecutionStrategy
{
    public PipelineResult<string> Execute() => PipelineResult<string>.Success("Reverted");
}

public static class TaskStrategyFactory
{
    public static ITaskExecutionStrategy Create(TaskStage stage) => stage switch
    {
        TaskStage.Gather => new GatherStrategy(),
        TaskStage.Validate => new ValidateStrategy(),
        TaskStage.Commit => new CommitStrategy(),
        TaskStage.Revert => new RevertStrategy(),
        _ => throw new ArgumentOutOfRangeException(nameof(stage))
    };
}
