using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MetricsPipeline.Core;
using MetricsPipeline.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace MetricsPipeline.ConsoleApp;

/// <summary>
/// Hosted worker that executes the demo pipeline and returns the fetched DTO list.
/// </summary>
public class GenericMetricsWorker : BackgroundService, IHostedWorker<GenericMetricsWorker.MetricDto>
{
    private readonly IPipelineOrchestrator _orchestrator;
    private IReadOnlyList<MetricDto> _items = Array.Empty<MetricDto>();

    public GenericMetricsWorker(IPipelineOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// DTO returned from the demo API.
    /// </summary>
    public record MetricDto { public double Value { get; set; } }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MetricDto>> RunAsync(CancellationToken ct = default)
    {
        var source = new Uri("https://api.example.com/data");
        var result = await _orchestrator.ExecuteAsync<MetricDto>(
            "demo", source, x => x.Value, SummaryStrategy.Average, 5.0, ct);
        _items = result.IsSuccess ? result.Value.RawItems.ToList() : Array.Empty<MetricDto>();
        return _items;
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => RunAsync(stoppingToken);
}
