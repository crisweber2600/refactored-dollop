// Console application entry point running the metrics pipeline demo.
using MetricsPipeline.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using MetricsPipeline.Core;
using Microsoft.Extensions.DependencyInjection;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddMetricsPipeline(o => o.UseInMemoryDatabase("demo"));
        services.AddHttpClient<HttpMetricsClient>(c =>
        {
            c.BaseAddress = new Uri("http://localhost:5000");
        });
        services.AddTransient<IGatherService, HttpGatherService>();
        services.AddHostedService<PipelineWorker>();
    })
    .Build();

await host.RunAsync();
