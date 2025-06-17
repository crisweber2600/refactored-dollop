using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MetricsPipeline.Core;

namespace MetricsPipeline.Infrastructure;

/// <summary>
/// Worker service that retrieves typed items using <see cref="HttpMetricsClient"/>.
/// </summary>
public class HttpWorkerService : IWorkerService
{
    private readonly HttpMetricsClient _client;
    public Uri Source { get; set; } = new("https://api.example.com/data");

    public HttpWorkerService(HttpMetricsClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async Task<PipelineResult<IReadOnlyList<T>>> FetchAsync<T>(CancellationToken ct = default)
    {
        try
        {
            var result = await _client.SendAsync<T>(HttpMethod.Get, Source.ToString(), ct);
            return PipelineResult<IReadOnlyList<T>>.Success(result!);
        }
        catch (Exception ex)
        {
            return PipelineResult<IReadOnlyList<T>>.Failure(ex.Message);
        }
    }
}
