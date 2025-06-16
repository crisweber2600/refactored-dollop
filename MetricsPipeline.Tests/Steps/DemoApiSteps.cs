using Reqnroll;
using MetricsPipeline.Infrastructure;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;

[Binding]
[Scope(Feature="DemoApiIntegration")]
public class DemoApiSteps
{
    private readonly HttpMetricsClient _client;
    private IReadOnlyList<double>? _result;
    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<MetricsPipeline.DemoApi.Program>? _factory;

    public DemoApiSteps(HttpMetricsClient client)
    {
        _client = client;
    }

    [Given("the demo API is running")]
    public void GivenApiRunning()
    {
        _factory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<MetricsPipeline.DemoApi.Program>();
    }
    [When(@"the http client requests ""(.*)"" using GET")]

    public async Task WhenClientRequests(string uri)
    {
        if (_factory != null)
        {
            var client = _factory.CreateClient();
            var metricsClient = new HttpMetricsClient(client);
            _result = await metricsClient.SendAsync<double>(HttpMethod.Get, uri);
        }
        else
        {
            _result = await _client.SendAsync<double>(HttpMethod.Get, uri);
        }
    }

    [Then(@"the response list should contain (\d+) items")]
    public void ThenListCount(int count)
    {
        _result.Should().NotBeNull();
        _result!.Count.Should().Be(count);
    }
}
