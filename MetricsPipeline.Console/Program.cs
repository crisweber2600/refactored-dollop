// Console application entry point running the metrics pipeline demo.
using MetricsPipeline.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddMetricsPipeline(o => o.UseInMemoryDatabase("demo"));
        services.AddHostedService<PipelineWorker>();
    })
    .Build();

await host.RunAsync();
