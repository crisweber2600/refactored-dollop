using MetricsPipeline.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddMetricsPipeline(o => o.UseInMemoryDatabase("demo"));
    })
    .Build();

await host.RunAsync();

