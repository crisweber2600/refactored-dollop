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
public class GenericMetricsWorker : BackgroundService, IHostedWorker<GenericMetricsWorker.MetricDto>, IWorkerService
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly IGatherService _gather;
    private IReadOnlyList<MetricDto> _items = Array.Empty<MetricDto>();

    /// <summary>
    /// Name of the worker method used by this demo worker.
    /// </summary>
    public const string WorkerMethod = nameof(FetchAsync);

    public Uri Source { get; set; } = new("https://api.example.com/data");

    public GenericMetricsWorker(IPipelineOrchestrator orchestrator, IGatherService gather)
    {
        _orchestrator = orchestrator;
        _gather = gather;
    }

    /// <summary>
    /// DTO returned from the demo API.
    /// </summary>
    public record MetricDto { public double Value { get; set; } }

    /// <summary>
    /// Fetches metric DTOs from the configured gather service.
    /// </summary>
    public async Task<PipelineResult<IReadOnlyList<T>>> FetchAsync<T>(CancellationToken ct = default)
    {
        if (typeof(T) != typeof(MetricDto))
            return PipelineResult<IReadOnlyList<T>>.Failure("UnsupportedType");
        var numbers = await _gather.FetchMetricsAsync(ct);
        if (!numbers.IsSuccess)
            return PipelineResult<IReadOnlyList<T>>.Failure(numbers.Error!);
        var list = numbers.Value!.Select(v => new MetricDto { Value = v }).ToList();
        return PipelineResult<IReadOnlyList<T>>.Success((IReadOnlyList<T>)list);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MetricDto>> RunAsync(CancellationToken ct = default)
    {
        var result = await _orchestrator.ExecuteAsync<MetricDto>(
            "demo",
            x => x.Value,
            SummaryStrategy.Average,
            5.0,
            ct,
            WorkerMethod);
        var message = result.IsSuccess ? "Committed" : "Reverted";
        System.Console.WriteLine(message);
        _items = result.IsSuccess ? result.Value.RawItems.ToList() : Array.Empty<MetricDto>();
        return _items;
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => RunAsync(stoppingToken);
}
