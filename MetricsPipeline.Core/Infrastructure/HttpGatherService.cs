namespace MetricsPipeline.Infrastructure;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MetricsPipeline.Core;

public class HttpGatherService : IGatherService
{
    private readonly HttpMetricsClient _client;

    public HttpGatherService(HttpMetricsClient client)
    {
        _client = client;
    }

    public async Task<PipelineResult<IReadOnlyList<double>>> FetchMetricsAsync(Uri source, CancellationToken ct = default)
    {
        try
        {
            var data = await _client.SendAsync<double>(HttpMethod.Get, source.ToString(), ct);
            return PipelineResult<IReadOnlyList<double>>.Success(data!);
        }
        catch (Exception ex)
        {
            return PipelineResult<IReadOnlyList<double>>.Failure(ex.Message);
        }
    }

    public Task<PipelineResult<IReadOnlyList<double>>> CustomGatherAsync(Uri source, CancellationToken ct = default)
        => FetchMetricsAsync(source, ct);
}
