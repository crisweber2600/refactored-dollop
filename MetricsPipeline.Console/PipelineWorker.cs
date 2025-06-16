using MetricsPipeline.Core;
using Microsoft.Extensions.Hosting;

namespace MetricsPipeline.Infrastructure;

/// <summary>
/// Hosted service that runs the demo metrics pipeline.
/// </summary>
public class PipelineWorker : BackgroundService
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly List<string> _executed = new();

    /// <summary>
    /// Gets the messages returned by each executed stage.
    /// </summary>
    public IReadOnlyList<string> ExecutedStages => _executed;

    /// <summary>
    /// Initializes a new instance of the worker.
    /// </summary>
    /// <param name="orchestrator">Pipeline orchestrator instance.</param>
    public PipelineWorker(IPipelineOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        RunStage(TaskStage.Gather);
        RunStage(TaskStage.Validate);

        var source = new Uri("https://api.example.com/data");
        var result = await _orchestrator.ExecuteAsync("demo", source, SummaryStrategy.Average, 5.0, stoppingToken);

        RunStage(result.IsSuccess ? TaskStage.Commit : TaskStage.Revert);
    }

    private void RunStage(TaskStage stage)
    {
        var strategy = TaskStrategyFactory.Create(stage);
        var outcome = strategy.Execute();
        _executed.Add(outcome.Value!);
        Console.WriteLine(outcome.Value);
    }
}
