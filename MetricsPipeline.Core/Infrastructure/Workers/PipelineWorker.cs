using MetricsPipeline.Core;
using Microsoft.Extensions.Hosting;

namespace MetricsPipeline.Infrastructure;

/// <summary>
/// Hosted service that runs the demo metrics pipeline.
/// </summary>
public class PipelineWorker : BackgroundService, IHostedWorker<string>
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
        var result = await _orchestrator.ExecuteAsync<MetricDto>(
            "demo",
            x => x.Value,
            SummaryStrategy.Average,
            5.0,
            stoppingToken);

        var message = result.IsSuccess ? "Committed" : "Reverted";
        _executed.Add(message);
        Console.WriteLine(message);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> RunAsync(CancellationToken ct = default)
    {
        await ExecuteAsync(ct);
        return ExecutedStages;
    }
}

internal record MetricDto
{
    public double Value { get; set; }
}
