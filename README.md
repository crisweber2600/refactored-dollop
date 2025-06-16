# Metrics Pipeline Demo

This project demonstrates a simple yet fully testable metrics processing pipeline written in **.NET 9**. The code is split across a core library, infrastructure layer and a console host used to showcase the pipeline in action. Behaviour driven tests cover the main scenarios and act as living documentation.

## Projects

| Project | Description |
|---------|-------------|
| **MetricsPipeline.Core** | Domain interfaces, pipeline orchestration logic and Entity Framework Core infrastructure. |
| **MetricsPipeline.Console** | Console application that wires the pipeline together and runs a sample workflow. |
| **MetricsPipeline.Tests** | xUnit/Reqnroll test suite validating each stage and the end-to-end flow. |

## Getting Started

1. **Build the solution**
   ```bash
   dotnet build
   ```
2. **Run the sample console application**
   ```bash
   dotnet run --project MetricsPipeline.Console
   ```
3. **Execute the tests**
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

## Console Application

`MetricsPipeline.Console` registers the pipeline services with a dependency injection container and runs `PipelineWorker`, a hosted service that executes the pipeline once at startup. The worker output demonstrates how each stage is called and whether the final summary is persisted or discarded.

## Testing

Behaviour driven tests under `MetricsPipeline.Tests` describe the expected behaviour in feature files and implement step definitions with Reqnroll. The test suite exercises:

- Gathering metrics from various endpoints
- Summarising values using different strategies
- Validating summaries against previous results
- Committing or discarding based on validation outcome
- Repository and unit-of-work behaviour

Running `dotnet test` executes all scenarios and the supporting unit tests.

## Contributing

Contributions are welcome. Please ensure that the solution builds and the tests pass before opening a pull request.

## License

This repository is provided for demonstration purposes without a specific license.
