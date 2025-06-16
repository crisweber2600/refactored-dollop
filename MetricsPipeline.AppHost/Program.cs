using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject("demoapi", "../MetricsPipeline.DemoApi/MetricsPipeline.DemoApi.csproj");
builder.AddProject("worker", "../MetricsPipeline.Console/MetricsPipeline.Console.csproj");

builder.Build().Run();
