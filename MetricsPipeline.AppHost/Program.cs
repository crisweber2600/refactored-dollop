using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var demo = builder.AddProject("demoapi", "../MetricsPipeline.DemoApi/MetricsPipeline.DemoApi.csproj")
    .WithHttpEndpoint();

builder.AddProject("worker", "../MetricsPipeline.Console/MetricsPipeline.Console.csproj")
    .WithReference(demo);

builder.Build().Run();
