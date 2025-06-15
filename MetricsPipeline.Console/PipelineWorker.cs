using MetricsPipeline.Core;
using Microsoft.Extensions.Hosting;

namespace MetricsPipeline.Infrastructure;

public class PipelineWorker : BackgroundService
{
    private readonly IPipelineOrchestrator _orchestrator;

    public PipelineWorker(IPipelineOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var source = new Uri("https://api.example.com/data");
        var result = await _orchestrator.ExecuteAsync(source, SummaryStrategy.Average, 5.0, stoppingToken);
        Console.WriteLine($"Success: {result.IsSuccess}");
        Console.WriteLine($"Summary: {result.Value.Summary}");
    }
}
