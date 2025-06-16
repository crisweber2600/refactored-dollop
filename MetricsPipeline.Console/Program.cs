// Console application entry point running the metrics pipeline demo.
using MetricsPipeline.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using MetricsPipeline.Core;
using Microsoft.Extensions.DependencyInjection;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddMetricsPipeline(o => o.UseInMemoryDatabase("demo"));
        services.AddHttpClient<HttpMetricsClient>(c =>
        {
            var discovered = context.Configuration["services:demoapi:0"] ??
                              context.Configuration["services:demoapi"];
            if (!string.IsNullOrEmpty(discovered))
            {
                c.BaseAddress = new Uri(discovered);
            }
        });
        services.AddTransient<IGatherService, HttpGatherService>();
        services.AddHostedService<PipelineWorker>();
    })
    .Build();

await host.RunAsync();
