namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;

/// <summary>
/// Simple gather service that serves metrics from an in-memory dictionary.
/// </summary>
public class InMemoryGatherService : IGatherService
{
    private readonly IDictionary<Uri, IReadOnlyList<double>> _dataMap =
        new Dictionary<Uri, IReadOnlyList<double>>
        {
            [new Uri("https://api.example.com/data")] = new[] {44.5,45.0,45.5},
            [new Uri("https://api.example.com/empty")] = Array.Empty<double>()
        };

    /// <summary>
    /// Registers or replaces a metric endpoint.
    /// </summary>
    public void RegisterEndpoint(Uri uri, IReadOnlyList<double> values) => _dataMap[uri] = values;

    /// <summary>
    /// Removes a previously registered endpoint.
    /// </summary>
    public void RemoveEndpoint(Uri uri) { if (_dataMap.ContainsKey(uri)) _dataMap.Remove(uri); }

    /// <inheritdoc />
    public Task<PipelineResult<IReadOnlyList<double>>> FetchMetricsAsync(Uri source, CancellationToken ct = default)
    {
        if (!_dataMap.TryGetValue(source, out var set))
            return Task.FromResult(PipelineResult<IReadOnlyList<double>>.Failure("DataUnavailable"));
        return set.Count == 0
            ? Task.FromResult(PipelineResult<IReadOnlyList<double>>.Failure("NoData"))
            : Task.FromResult(PipelineResult<IReadOnlyList<double>>.Success(set));
    }

    /// <summary>
    /// Alternate gather method used for testing dynamic selection.
    /// </summary>
    public Task<PipelineResult<IReadOnlyList<double>>> CustomGatherAsync(Uri source, CancellationToken ct = default)
        => FetchMetricsAsync(source, ct);
}
