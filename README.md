# Metrics Pipeline Demo

This repository showcases a small metrics processing pipeline built with .NET. It includes a reusable core library, an infrastructure layer with Entity Framework storage, a console host and a comprehensive suite of BDD-style tests.

## Projects

| Project | Description |
|---------|-------------|
| **MetricsPipeline.Core** | Domain abstractions and EF based infrastructure. |
| **MetricsPipeline.Console** | Console application demonstrating the pipeline. |
| **MetricsPipeline.Tests** | xUnit/Reqnroll tests covering the pipeline behaviour. |

## Getting Started

1. **Restore and Build**
   ```bash
   dotnet build
   ```
2. **Run the demo console app**
   ```bash
   dotnet run --project MetricsPipeline.Console
   ```
3. **Execute the test suite**
   ```bash
   dotnet test
   ```

The console application retrieves sample metric values from an in-memory source, calculates a summary and either commits or discards the result based on a threshold check. The output of each pipeline stage is printed to the console.

## Architecture

The pipeline is composed of several services defined in `MetricsPipeline.Core`:

- **Gather Service** – fetches raw metrics from a source.
- **Summarization Service** – calculates a summary (average, sum or count).
- **Validation Service** – ensures the new summary is within an acceptable delta of the previous value.
- **Commit Service** – persists valid summaries.
- **Discard Handler** – invoked when a summary is outside the allowed threshold.

The `PipelineOrchestrator` coordinates these services. For persistence an EF Core `SummaryDbContext` is used. The tests cover each stage and integration scenarios.

## Contributing

Pull requests are welcome. Ensure all tests pass before submitting changes.

## License

This project is provided for demonstration purposes only and has no specific license.
