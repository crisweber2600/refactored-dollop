namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Simple gather service that serves metric values from an in-memory list.
/// </summary>
public class ListGatherService : IGatherService, IWorkerService
{
    private IReadOnlyList<double> _metrics = Array.Empty<double>();

    /// <summary>Metrics returned by <see cref="FetchMetricsAsync"/>.</summary>
    public IReadOnlyList<double> Metrics
    {
        get => _metrics;
        set => _metrics = value ?? Array.Empty<double>();
    }

    /// <inheritdoc />
    public Task<PipelineResult<IReadOnlyList<double>>> FetchMetricsAsync(CancellationToken ct = default)
    {
        if (_metrics.Count == 0)
            return Task.FromResult(PipelineResult<IReadOnlyList<double>>.Failure("NoData"));
        return Task.FromResult(PipelineResult<IReadOnlyList<double>>.Success(_metrics));
    }

    /// <inheritdoc />
    public Task<PipelineResult<IReadOnlyList<T>>> FetchAsync<T>(CancellationToken ct = default)
    {
        if (typeof(T) == typeof(double))
        {
            var result = FetchMetricsAsync(ct);
            return result.ContinueWith(t =>
                t.Result.IsSuccess
                    ? PipelineResult<IReadOnlyList<T>>.Success((IReadOnlyList<T>)(object)t.Result.Value!)
                    : PipelineResult<IReadOnlyList<T>>.Failure(t.Result.Error!), ct);
        }

        var prop = typeof(T).GetProperty("Value") ?? typeof(T).GetProperty("Amount");
        if (prop == null || prop.PropertyType != typeof(double))
            return Task.FromResult(PipelineResult<IReadOnlyList<T>>.Failure("UnsupportedType"));

        var list = _metrics.Select(v =>
        {
            var inst = Activator.CreateInstance(typeof(T));
            prop.SetValue(inst, v);
            return (T)inst!;
        }).ToList();

        return list.Count == 0
            ? Task.FromResult(PipelineResult<IReadOnlyList<T>>.Failure("NoData"))
            : Task.FromResult(PipelineResult<IReadOnlyList<T>>.Success(list));
    }

    /// <summary>
    /// Alternate gather method used for testing custom worker methods.
    /// </summary>
    public Task<PipelineResult<IReadOnlyList<double>>> CustomGatherAsync(CancellationToken ct = default)
        => FetchMetricsAsync(ct);
}
