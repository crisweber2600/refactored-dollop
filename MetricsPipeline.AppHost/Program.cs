var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.MetricsPipeline_DemoApi>("metricspipeline-demoapi");

builder.AddProject<Projects.MetricsPipeline_Console>("metricspipeline-console");

builder.Build().Run();
