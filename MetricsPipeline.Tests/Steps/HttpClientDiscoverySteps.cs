using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MetricsPipeline.Infrastructure;
using Reqnroll;

[Binding]
[Scope(Feature="HttpClientServiceDiscovery")]
public class HttpClientDiscoverySteps
{
    private HttpMetricsClient? _client;

    [Given("service discovery variable is \"(.*)\"")]
    public void GivenServiceDiscoveryVariable(string uri)
    {
        Environment.SetEnvironmentVariable("services__demoapi__0", uri);
    }

    [When("the metrics client is created")]
    public void WhenClientCreated()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddHttpClient<HttpMetricsClient>(c =>
                {
                    var baseAddress = context.Configuration["services:demoapi:0"];
                    if (!string.IsNullOrEmpty(baseAddress))
                    {
                        c.BaseAddress = new Uri(baseAddress);
                    }
                });
            })
            .Build();
        _client = host.Services.GetRequiredService<HttpMetricsClient>();
    }

    [Then("the metrics client base address should be \"(.*)\"")]
    public void ThenBaseAddress(string expected)
    {
        _client.Should().NotBeNull();
        _client!.BaseAddress!.ToString().Should().Be(expected);
    }
}
