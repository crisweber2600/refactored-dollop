var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/metrics", () => new[] { 42.0, 43.1, 41.7 })
   .WithName("GetMetrics");

app.Run();

namespace MetricsPipeline.DemoApi
{
    public partial class Program { }
}
