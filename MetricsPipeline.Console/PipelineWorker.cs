using MetricsPipeline.Core;
using Microsoft.Extensions.Hosting;

namespace MetricsPipeline.Infrastructure;

/// <summary>
/// Hosted service that runs the demo metrics pipeline.
/// </summary>
public class PipelineWorker : BackgroundService
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly IGatherService _gather;
    private readonly List<string> _executed = new();
    private IReadOnlyList<double>? _gathered;

    /// <summary>
    /// Gets the messages returned by each executed stage.
    /// </summary>
    public IReadOnlyList<string> ExecutedStages => _executed;

    /// <summary>
    /// Initializes a new instance of the worker.
    /// </summary>
    /// <param name="orchestrator">Pipeline orchestrator instance.</param>
    /// <param name="gather">Gather service used by the worker.</param>
    public PipelineWorker(IPipelineOrchestrator orchestrator, IGatherService gather)
    {
        _orchestrator = orchestrator;
        _gather = gather;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunStageAsync(TaskStage.Gather, stoppingToken);
        await RunStageAsync(TaskStage.Validate, stoppingToken);

        var source = new Uri("/metrics", UriKind.Relative);
        var result = await _orchestrator.ExecuteAsync("demo", source, SummaryStrategy.Average, 5.0, stoppingToken);

        await RunStageAsync(result.IsSuccess ? TaskStage.Commit : TaskStage.Revert, stoppingToken);
    }

    private async Task RunStageAsync(TaskStage stage, CancellationToken ct)
    {
        switch (stage)
        {
            case TaskStage.Gather:
                var gatherResult = await _gather.FetchMetricsAsync(new Uri("/metrics", UriKind.Relative), ct);
                _gathered = gatherResult.IsSuccess ? gatherResult.Value : Array.Empty<double>();
                _executed.Add("Gathered");
                Console.WriteLine("Gathered");
                break;
            case TaskStage.Validate:
                var isValid = _gathered != null && _gathered.Count > 0;
                _executed.Add(isValid ? "Validated" : "ValidationFailed");
                Console.WriteLine(isValid ? "Validated" : "ValidationFailed");
                break;
            default:
                var strategy = TaskStrategyFactory.Create(stage);
                var outcome = strategy.Execute();
                _executed.Add(outcome.Value!);
                Console.WriteLine(outcome.Value);
                break;
        }
    }
}
