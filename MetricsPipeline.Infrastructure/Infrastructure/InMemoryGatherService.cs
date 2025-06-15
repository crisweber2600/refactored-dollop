namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;

public class InMemoryGatherService : IGatherService
{
    private readonly IDictionary<Uri, IReadOnlyList<double>> _dataMap =
        new Dictionary<Uri, IReadOnlyList<double>>
        {
            [new Uri("https://api.example.com/data")] = new[] {44.5,45.0,45.5},
            [new Uri("https://api.example.com/empty")] = Array.Empty<double>()
        };

    public Task<PipelineResult<IReadOnlyList<double>>> FetchMetricsAsync(Uri source, CancellationToken ct = default)
    {
        if (!_dataMap.TryGetValue(source, out var set))
            return Task.FromResult(PipelineResult<IReadOnlyList<double>>.Failure("DataUnavailable"));
        return set.Count == 0
            ? Task.FromResult(PipelineResult<IReadOnlyList<double>>.Failure("NoData"))
            : Task.FromResult(PipelineResult<IReadOnlyList<double>>.Success(set));
    }
}
