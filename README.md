# Metrics Pipeline Demo

This project demonstrates a simple yet fully testable metrics processing pipeline written in **.NET 9**. The code is split across a core library, infrastructure layer and a console host used to showcase the pipeline in action. Behaviour driven tests cover the main scenarios and act as living documentation.

## Projects

| Project | Description |
|---------|-------------|
| **MetricsPipeline.Core** | Domain interfaces, pipeline orchestration logic and Entity Framework Core infrastructure. |
| **MetricsPipeline.Console** | Console application that wires the pipeline together and runs a sample workflow. |
| **MetricsPipeline.Tests** | xUnit/Reqnroll test suite validating each stage and the end-to-end flow. |
| **MetricsPipeline.DemoApi** | Minimal API returning sample metrics for the worker to consume. |
| **MetricsPipeline.AppHost** | Dotnet Aspire host that runs the demo API and worker together. |

## Getting Started

1. **Build the solution**
   ```bash
   dotnet build
   ```
2. **Apply migrations if new entities have been added**
   ```bash
   dotnet ef migrations add <name> --project MetricsPipeline.Core
   ```
   Updating the database can then be performed manually when required.
3. **Run the Aspire host**
   ```bash
   dotnet run --project MetricsPipeline.AppHost
   ```
4. **Run the sample console application alone**
   ```bash
   dotnet run --project MetricsPipeline.Console
   ```
5. **Execute the tests**
   ```bash
   dotnet test
   ```

The console host fetches a small set of metric values from an in-memory source, summarises them and either commits the result or discards it depending on validation. Each stage writes its status to the console.

## Architecture Overview

At a high level the pipeline flows through a fixed sequence of services coordinated by the `PipelineOrchestrator`:

```
 [GatherService] -> [SummarizationService] -> [ValidationService] -> [CommitService]
                                                          |
                                                          v
                                                  [DiscardHandler]
```

1. **GatherService** – retrieves the raw metrics from a configured endpoint.
2. **SummarizationService** – aggregates the metrics using a chosen strategy (average, sum or count).
3. **ValidationService** – compares the new summary to the last committed value and checks the delta against an acceptable threshold.
4. **CommitService** – persists valid summaries through the repository and database context.
5. **DiscardHandler** – invoked when validation fails so the summary can be logged or otherwise handled.

The orchestrator invokes these services in order and returns a `PipelineState` object describing the entire run.

### Service Implementations

The default implementations live in `MetricsPipeline.Infrastructure`:

- `InMemoryGatherService` – serves metric values from an in-memory dictionary and is used for both tests and the demo console.
- `InMemorySummarizationService` – performs calculations in memory and supports the `Average`, `Sum` and `Count` strategies.
- `ThresholdValidationService` – checks that the difference between the current and previous summaries does not exceed a supplied threshold.
- `EfCommitService` – saves summaries to the database by calling `EfSummaryRepository`.
- `LoggingDiscardHandler` – simply writes the discarded summary to the console.

Entity Framework Core is used for persistence via the `SummaryDbContext`. Summary records are stored with a timestamp and may be soft deleted.

### Orchestrator Flow

```
+--------------+     +--------------------+     +-------------------+
| Gather       | --> | Summarize Metrics  | --> | Validate Threshold |
+--------------+     +--------------------+     +----------+--------+
                                                   |  yes
                                                   v
                                           +-------+-------+
                                           | Commit Result |
                                           +---------------+
                                                   ^  no
                                                   |
                                           +-------+-------+
                                           |  Discard Log  |
                                           +---------------+
```

The orchestrator collects metrics, summarises them, retrieves the last committed value from `EfSummaryRepository` and validates the delta. If validation passes it commits the new summary. Otherwise the discard handler is triggered.

When no prior summary exists the orchestrator now treats the run as valid regardless of the delta, ensuring that the first execution of a new pipeline always commits its results.

## Console Application

`MetricsPipeline.Console` registers the pipeline services with a dependency injection container and runs `PipelineWorker`, a hosted service that executes the pipeline once at startup. The worker output demonstrates how each stage is called and whether the final summary is persisted or discarded.

The worker can be customised by supplying an alternative gather method name when invoking the orchestrator. A single worker can host several pipelines targeting different data types so multiple gather methods may run side by side. Each pipeline has its own threshold and summarisation strategy, making it simple to plug the library into new domains without rewriting the worker service.

The `MetricsPipeline.DemoApi` project exposes a minimal `/metrics` endpoint returning sample values. A reusable `HttpMetricsClient` abstracts `HttpClient` so the worker can fetch data from any URI using any HTTP method and deserialize it into a list of typed objects. When running under `MetricsPipeline.AppHost` the console worker automatically calls the demo API through this client.

## Database Migrations

Entity Framework Core migrations are included with the project. Ensure the `dotnet-ef` tool is installed:

```bash
dotnet tool install --global dotnet-ef
```

Run migrations whenever the models change:

```bash
dotnet ef migrations add <name> --project MetricsPipeline.Core
```
The database should be updated manually only when required rather than as part of every test run.
Seed data files can be placed in the `MetricsPipeline.Console/Seed` folder and will be imported on startup if they do not already exist in the database.

## Extending the Pipeline

The default services can be replaced with custom implementations through dependency injection. Implement the required interfaces and register the services before running the worker. For example, a custom gather method can be provided via:

```csharp
services.AddScoped<IGatherService, MyGatherService>();
```

Additional summarisation strategies can be registered in the same way to tailor the pipeline to new data sources.

You can also reuse `HttpMetricsClient` in your own services to call REST endpoints by specifying the HTTP method and target URI. The client returns a strongly typed list so it works with any DTO shape.

You can also extend the validation logic by implementing `IValidationService`. The default implementation can summarise any `List<T>` by projecting a property with a LINQ expression. Register your custom service before running the worker to apply domain-specific rules or alternative summarisation logic.

## Testing

Behaviour driven tests under `MetricsPipeline.Tests` describe the expected behaviour in feature files and implement step definitions with Reqnroll. The test suite exercises:

- Gathering metrics from various endpoints
- Summarising values using different strategies
- Validating summaries against previous results
- Committing or discarding based on validation outcome
- Repository and unit-of-work behaviour

Running `dotnet test` executes all scenarios and the supporting unit tests. Database migrations only need to be applied when entity models change:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Contributing

Contributions are welcome. Please ensure that the solution builds and the tests pass before opening a pull request.

## License

This repository is provided for demonstration purposes without a specific license.
