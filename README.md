# Metrics Pipeline Demo

This project demonstrates a simple yet fully testable metrics processing pipeline written in **.NET 9**. The code is split across a core library, infrastructure layer and a console host used to showcase the pipeline in action. Behaviour driven tests cover the main scenarios and act as living documentation. Components communicate through Aspire's built-in service discovery.

## Projects

| Project | Description |
|---------|-------------|
| **MetricsPipeline.Core** | Domain interfaces, pipeline orchestration logic and Entity Framework Core infrastructure. Includes reusable worker implementations under `Infrastructure/Workers`. |
| **MetricsPipeline.Console** | Console application that wires the pipeline together and runs a sample workflow. |
| **MetricsPipeline.Tests** | xUnit/Reqnroll test suite validating each stage and the end-to-end flow. |
| **MetricsPipeline.DemoApi** | Minimal API returning sample metrics for the worker to consume. |
| **MetricsPipeline.AppHost** | Dotnet Aspire host that runs the demo API and worker together using service discovery. |

## Getting Started

1. **Build the solution**
   ```bash
   dotnet build
   ```
   If packages were not restored previously run `dotnet restore` first.
2. **Apply migrations if new entities have been added**
   ```bash
   dotnet ef migrations add <name> --project MetricsPipeline.Core
   ```
   Updating the database can then be performed manually when required.
3. **Run the Aspire host**
   ```bash
   dotnet run --project MetricsPipeline.AppHost
   ```
   This host links the console project to the demo API using `.WithReference`,
   so the API address is discovered automatically.
4. **Run the sample console application alone**
   ```bash
   dotnet run --project MetricsPipeline.Console
   # for automatic rebuilds use
   # dotnet watch run --project MetricsPipeline.Console
   ```
   When not using the Aspire host specify the Demo API address manually:
   ```bash
   services__demoapi__0="https://localhost:5050" dotnet run --project MetricsPipeline.Console
   ```
   The console reads the `services__demoapi__0` variable to discover the API.
5. **Execute the tests**
   ```bash
   dotnet test --no-restore --no-build
   ```
   6. **Define custom pipelines**
   Configure additional gather methods and thresholds as needed by your application.

7. **Select a database provider**
   Set `DB_PROVIDER=sqlite` before running the console or tests to use an in-memory SQLite database instead of the default provider.
   Run `git clean -xfd` after switching providers so the context is rebuilt from a clean slate.

8. **Run a specific scenario**
   Use `dotnet test --filter "<name>"` to execute an individual feature when debugging.

9. **Review AGENTS.md after each task**
   Capture lessons learned and update the guidelines for next time.
10. **Confirm environment variables**
   Use `printenv services__demoapi__0` to verify the API address is set.
   Set `DOTNET_CLI_TELEMETRY_OPTOUT=1` to disable telemetry during automated runs.
11. **Add custom workers**
   Place new worker classes in `MetricsPipeline.Core/Infrastructure/Workers` so they can be reused by multiple hosts.
12. **Use pipeline options**
   `AddMetricsPipeline` can now register the hosted worker and HTTP client automatically when configured.
13. **Enable the worker via options**
   Pass `opts.AddWorker = true` when calling `AddMetricsPipeline` to run `PipelineWorker` as a background service.
14. **Specify a worker type**
   Use `opts.WorkerType = typeof(GenericMetricsWorker)` or call `AddMetricsPipeline(typeof(GenericMetricsWorker), ...)` to register a custom hosted worker.
15. **Choose a worker mode**
    Set `opts.WorkerMode = WorkerMode.Http` to use the HTTP worker or leave it as the default mode which relies on `ListGatherService`.
16. **Register the HTTP client**
   Set `opts.RegisterHttpClient = true` to make `HttpMetricsClient` available for custom services.
17. **Customise the HTTP client**
   Use `opts.ConfigureClient` to set `BaseAddress` or other settings after service discovery.
18. **Specify a worker method**
   Pass the `workerMethod` name to `ExecuteAsync` when calling the orchestrator.
   The demo worker exposes `GenericMetricsWorker.WorkerMethod` for this purpose.
19. **Run tests without restore**
   Invoke `dotnet test --no-restore --no-build` for faster execution once packages are restored.
20. **Demo worker properties**
   `GenericMetricsWorker` exposes a `Source` URI and constant `WorkerMethod` so hosts need minimal setup.
21. **Flexible worker signatures**
   `ExecuteAsync` now invokes worker methods without supplying a source value.
22. **Configure the source**
   Set `GenericMetricsWorker.Source` to control where metrics are retrieved from.

Example configuration enabling the HTTP worker:

```csharp
services.AddMetricsPipeline(
    o => o.UseInMemoryDatabase("demo"),
    opts =>
    {
        opts.WorkerMode = WorkerMode.Http;
        opts.RegisterHttpClient = true;
    });
```
You can also specify the worker type:

```csharp
services.AddMetricsPipeline(
    typeof(GenericMetricsWorker),
    o => o.UseInMemoryDatabase("demo"),
    opts => opts.AddWorker = true);
```
Calling the orchestrator directly requires the worker method name:

```csharp
var result = await orchestrator.ExecuteAsync<MyDto>(
    "demo",
    x => x.Value,
    SummaryStrategy.Average,
    5.0,
    CancellationToken.None,
    GenericMetricsWorker.WorkerMethod);
```
The worker selects its own `Source` value so callers no longer supply one.
The console host fetches a small set of metric values from an in-memory source, summarises them and either commits the result or discards it depending on validation. Each stage writes its status to the console.
## Worker Registration and DI Options

- **AddWorker** registers `PipelineWorker` as a hosted service so metrics run in the background.
- **WorkerMode** selects between `InMemory` and `Http` gatherers.
- **RegisterHttpClient** makes `HttpMetricsClient` available without changing the gather service.
- **ConfigureClient** lets you set `HttpClient` properties such as `BaseAddress` after service discovery.
- **Worker classes** live under `MetricsPipeline.Core/Infrastructure/Workers` for reuse across hosts.
- **WorkerType** lets you specify an alternative hosted worker. Set `opts.WorkerType = typeof(MyWorker)` or use the overload `AddMetricsPipeline(typeof(MyWorker), ...)`.


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
6. **Worker method flexibility** – the orchestrator now calls any worker method that returns a list of items, even if it doesn't take the source parameter.

The orchestrator invokes these services in order and returns a `PipelineState` object describing the entire run.

### Service Implementations

- The default implementations live in `MetricsPipeline.Infrastructure`:

- `ListGatherService` – provides metric values from an in-memory collection and is used for both tests and the demo console.
- Both `IGatherService` and `IWorkerService` now resolve to the same scoped instance so step definitions and the orchestrator share data.
- `InMemorySummarizationService` – performs calculations in memory and supports the `Average`, `Sum` and `Count` strategies.
- `ThresholdValidationService` – checks that the difference between the current and previous summaries does not exceed a supplied threshold.
- `EfCommitService` – saves summaries to the database by calling `EfSummaryRepository`.
- `LoggingDiscardHandler` – simply writes the discarded summary to the console.
- Set `DB_PROVIDER=sqlite` to run the pipeline using an in-memory SQLite database for closer parity with production.

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

`MetricsPipeline.Console` registers the pipeline services with a dependency injection container and runs `PipelineWorker`, a hosted service defined in the core infrastructure. The worker no longer injects `IWorkerService`; it simply calls the orchestrator and writes whether the run was committed or reverted. This keeps the background task lightweight while retaining full reuse of the orchestrator.

```csharp
var result = await _orchestrator.ExecuteAsync<MetricDto>(
    "demo", x => x.Value, SummaryStrategy.Average, 5.0, ct);
Console.WriteLine(result.IsSuccess ? "Committed" : "Reverted");
```

The worker exposes an `ExecutedStages` list which now contains a single entry representing the final outcome. Success adds `Committed` while failure adds `Reverted`.
Additional notes:

* The previous `RunStageAsync` helper was removed. `ExecuteAsync` simply delegates to the orchestrator and logs the final state.
* You can tweak the summarisation strategy and threshold on each call without modifying the worker code.
* `PipelineResult.IsSuccess` indicates whether the summary was committed (`true`) or reverted (`false`).
* Custom discard handlers can observe failed results for auditing or alerting purposes.
* `MetricsPipelineOptions` exposes flags to register the worker and HTTP client so configuration remains minimal. A `WorkerMode` property controls whether metrics are gathered from memory or via HTTP.


### Running Multiple Pipelines

The worker can now be customised by supplying an alternative worker method when invoking the orchestrator. A single worker can host several pipelines targeting different DTOs so multiple methods may run side by side. Each pipeline has its own threshold and summarisation strategy, making it simple to plug the library into new domains without rewriting the worker service. Below is an example showing how two pipelines can be executed sequentially using different worker methods:

```csharp
await _orchestrator.ExecuteAsync<MetricDto>("demo", x => x.Value, SummaryStrategy.Average, 5.0, ct);
await _orchestrator.ExecuteAsync<MetricDto>("demo-alt", x => x.Value, SummaryStrategy.Sum, 10.0, ct, nameof(HttpWorkerService.FetchAsync));
```

Each call to `ExecuteAsync` selects the worker method by name allowing multiple pipelines to reuse the same orchestrator instance.
Different thresholds and strategies can be applied per pipeline so the same worker can handle diverse datasets.

All HTTP calls use relative paths which combine with the discovered service base address, keeping configuration minimal. You can inspect `HttpMetricsClient.BaseAddress` at runtime to confirm which endpoint was resolved so you know exactly which service the worker discovered. The worker service reuses `HttpMetricsClient` so any DTO can be downloaded and summarised without additional boilerplate.

The `MetricsPipeline.DemoApi` project exposes a minimal `/metrics` endpoint returning sample values. A reusable `HttpMetricsClient` abstracts `HttpClient` so the worker can fetch data from any URI using any HTTP method and deserialize it into a list of typed objects. When running under `MetricsPipeline.AppHost` the console worker automatically calls the demo API through this client.
The client now exposes a `BaseAddress` property so tests can verify which service endpoint was discovered.

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

The default services can be replaced with custom implementations through dependency injection. Additional summarisation strategies and validation rules may be registered in the same way to tailor the pipeline to new data sources.

### Registering a Custom IWorkerService
Implement your own `IWorkerService` to pull data from alternative sources. Register the implementation before running the worker:

```csharp
services.AddScoped<IWorkerService, MyWorkerService>();
```
When using the built-in `ListGatherService`, both interfaces resolve to the same scoped instance so registrations should follow the same pattern. Because the worker now delegates entirely to the orchestrator you can swap gather services without modifying the hosted service.

You can also reuse `HttpMetricsClient` in your own services to call REST endpoints by specifying the HTTP method and target URI. The client returns a strongly typed list so it works with any DTO shape. When a `services__<name>__0` environment variable is present the client automatically sets its base address, enabling simple service discovery between projects. When hosting multiple projects together you can add them in `MetricsPipeline.AppHost` and call `.WithReference()` so Aspire configures the discovery variables for you.

### Creating Hosted Workers
Implement `IHostedWorker<T>` for background tasks that return typed results. Register the worker via `opts.WorkerType` or by calling `AddMetricsPipeline(typeof(MyWorker), ...)`.

### Custom Validation Service
Implement `IValidationService` to enforce domain-specific checks:

```csharp
services.AddScoped<IValidationService, MyValidationService>();
```
Register your custom service before running the worker to apply alternative validation logic.

## Testing

- Gathering metrics from various endpoints
- Summarising values using different strategies
- Validating summaries against previous results
- Committing or discarding based on validation outcome
- Repository and unit-of-work behaviour
- Service discovery of the demo API via environment variables
- Interpreting the final worker message to confirm the pipeline outcome

Running `dotnet test` executes all scenarios and the supporting unit tests. Database migrations only need to be applied when entity models change:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Contributing

Contributions are welcome. Please ensure that the solution builds and the tests pass before opening a pull request.
- Keep the README updated with at least five improvements per pull request.
- Update `AGENTS.md` with lessons learned so future runs improve.
- Generate code coverage with `dotnet test --collect:"XPlat Code Coverage"` when possible.

## License

This repository is provided for demonstration purposes without a specific license.
