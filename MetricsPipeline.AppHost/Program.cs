var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.MetricsPipeline_DemoApi>("demoapi");

builder.AddProject<Projects.MetricsPipeline_Console>("metricspipeline-console").WithReference(api);

builder.Build().Run();
