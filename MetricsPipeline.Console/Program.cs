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
            // Changed key to reference the correct service discovery as configured in MetricsPipeline.AppHost\Program.cs.
            var discovered = context.Configuration["services:metricspipeline-demoapi:0"] ??
                             context.Configuration["services:metricspipeline-demoapi"];
            if (!string.IsNullOrEmpty(discovered))
            {
                c.BaseAddress = new Uri(discovered);
            }
        });
        services.AddTransient<IGatherService, HttpGatherService>();
        services.AddTransient<IWorkerService, HttpWorkerService>();
        services.AddHostedService<PipelineWorker>();
    })
    .Build();

await host.RunAsync();
