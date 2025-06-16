using System.Net.Http;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
namespace MetricsPipeline.Infrastructure;

public class HttpMetricsClient
{
    private readonly HttpClient _client;

    /// <summary>
    /// Gets the base address configured on the underlying <see cref="HttpClient"/>.
    /// </summary>
    public Uri? BaseAddress => _client.BaseAddress;

    public HttpMetricsClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<T>?> SendAsync<T>(HttpMethod method, string uri, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(method, uri);
        var response = await _client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        var data = System.Text.Json.JsonSerializer.Deserialize<List<T>>(json);
        return data ?? new List<T>();
    }
}
