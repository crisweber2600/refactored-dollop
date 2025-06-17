// Console application entry point running the metrics pipeline demo.
using System;
using Microsoft.Extensions.Configuration;
using MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddMetricsPipeline(
            o => o.UseInMemoryDatabase("demo"),
            opts =>
            {
                opts.AddWorker = true;
                opts.WorkerMode = WorkerMode.Http;
                opts.ConfigureClient = (sp, c) =>
                {
                    var cfg = sp.GetRequiredService<IConfiguration>();
                    var discovered = cfg["services:metricspipeline-demoapi:https:0"];
                    if (!string.IsNullOrEmpty(discovered))
                    {
                        c.BaseAddress = new Uri(discovered);
                    }
                };
            });
    })
    .Build();

await host.RunAsync();
