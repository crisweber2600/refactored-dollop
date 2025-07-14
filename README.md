# RAGStart

RAGStart showcases an event-driven validation pipeline built with .NET. The project exposes a small library called **ExampleLib** which demonstrates persisting and validating entities using either Entity Framework Core or MongoDB. The solution is intended as a reference for structuring data access, validation rules and test automation.

## Table of Contents
1. [Getting Started](#getting-started)
2. [Repository Structure](#repository-structure)
3. [Key Building Blocks](#key-building-blocks)
4. [Configuring the Data Layer](#configuring-the-data-layer)
5. [Validation Pipeline](#validation-pipeline)
6. [Manual Validation Service](#manual-validation-service)
7. [Validating Sequences](#validating-sequences)
8. [Loading Validation Flows](#loading-validation-flows)
9. [Running Tests](#running-tests)
10. [Why Choose RAGStart?](#why-choose-ragstart)
11. [More Documentation](#more-documentation)
12. [Troubleshooting](#troubleshooting)

## Getting Started
1. Install the [.NET&nbsp;9 SDK](https://dotnet.microsoft.com/en-us/download).
2. Set the environment variable `DOTNET_ROLL_FORWARD=Major` when using .NET 9 runtimes.
3. Run `dotnet build` then `dotnet test --collect:"XPlat Code Coverage"` to compile and execute all tests.
4. Reference `ExampleLib` in your own project and register services as shown below.
5. Use `AddManyAsync` and `UpdateManyAsync` for efficient bulk operations.

### Building with Different Providers
The solution supports Entity Framework Core or MongoDB. Configure the provider during startup:
```csharp
services.AddEfCoreRepositories<TestDbContext>();
// or
services.AddMongoRepositories(options => options.ConnectionString = "mongodb://localhost");
```

## Repository Structure
- `src/ExampleLib` – the domain models and infrastructure code.
- `tests/ExampleLib.Tests` – unit tests covering repositories and validators.
- `tests/ExampleLib.Tests/ExampleData` – sample entities and EF/Mongo configuration.
- *(BDD tests have been removed for clarity.)*
- `docs` – additional guides including `EFCoreReplicationGuide.md` and `Implementation.md`.

## Key Building Blocks
`SaveAudit` records details of each save, including the size of bulk operations:
```csharp
public class SaveAudit
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public decimal MetricValue { get; set; }
    public int BatchSize { get; set; }
    public bool Validated { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
```
Repositories implement `IGenericRepository<T>` for data access. Entity Framework and MongoDB variants provide the same interface for adding, updating and deleting entities.

## Configuring the Data Layer
The library registers services through extension methods. Use `AddExampleLib` to set up validation and data access in one step:
```csharp
services.AddExampleLib(builder =>
{
    builder.UseEf<TestDbContext>(options => options.UseSqlServer("connection"));
});
```
Switch to MongoDB by calling `UseMongo` instead. Both providers expose `AddBatchAudit` for summarising bulk saves.

## Validation Pipeline
Entities are validated against a `SummarisationPlan` whenever they are saved. The Entity Framework unit of work exposes `SaveChangesWithPlanAsync<TEntity>()`, while the MongoDB implementation uses an interceptor to apply the same logic during inserts and updates.

## Manual Validation Service
`ManualValidatorService` lets you register predicates to run against specific types:
```csharp
services.AddValidatorService()
        .AddValidatorRule<Order>(o => o.Total > 0);
```
All rules for a type must pass for the service to return `true`. You can inspect and modify the registered predicates via dependency injection. Combine manual predicates with summarisation rules using `AddSaveValidation`:
```csharp
services.AddSaveValidation<Order>(o => o.Total,
    ThresholdType.RawDifference, 2m,
    o => o.Status == "Open",
    o => o.Total > 0);
services.AddValidationRunner();
```
`AddValidationRunner` registers a single service that executes every validator so existing repositories can enable validation in one line.

## Validating Sequences
`SequenceValidator` compares successive items in a sequence. Provide key and value selectors with an optional comparison delegate:
```csharp
var ok = SequenceValidator.Validate(items, x => x.Server, x => x.Value,
    (cur, prev) => Math.Abs(cur - prev) < 10);
```
You can also drive the comparison using a `SummarisationPlan`:
```csharp
var plan = new SummarisationPlan<MyEntity>(e => e.Value, ThresholdType.RawDifference, 5);
bool passes = SequenceValidator.Validate(items, e => e.Server, plan);
```
Use `ThresholdValidator.IsWithinThreshold` in custom checks to maintain consistent logic.

## Loading Validation Flows
Validation rules can be configured from JSON:
```json
[
  {
    "Type": "ExampleData.YourEntity, ExampleLib.Tests",
    "SaveValidation": true,
    "MetricProperty": "Id",
    "ThresholdType": "RawDifference",
    "ThresholdValue": 2
  }
]
```
Register the configuration at startup:
```csharp
var json = File.ReadAllText("flows.json");
var options = ValidationFlowOptions.Load(json);
services.AddValidationFlows(options);
```
This hook wires up save, commit and delete validations automatically.

## Running Tests
Run the unit tests with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```
The generated coverage report appears under `TestResults` and should exceed 80%.

## Why Choose RAGStart?
- Provides a working example of event-driven validation with EF Core and MongoDB.
- Shows how to separate validation rules from persistence logic.
- Offers a one-line `AddValidationRunner` registration for existing repositories.
- Includes helper methods such as `AddManyAsync`, `UpdateManyAsync` and `AddBatchAudit` for efficient bulk operations.
- Demonstrates how to register per-entity rules using `AddSaveValidation`.
- Features a clear folder structure that can be reused in other projects.

## More Documentation
Further information is available in the `docs` folder:
- `EFCoreReplicationGuide.md` explains how to replicate the EF Core configuration.
- `Implementation.md` discusses class library design at different maturity levels.

## Troubleshooting
- Ensure `DOTNET_ROLL_FORWARD=Major` is set when targeting .NET 9.
- Run `dotnet restore` if packages fail to resolve.
- Delete `bin` and `obj` directories after SDK upgrades to avoid stale builds.
- MongoDB tests rely on **Mongo2Go**; check that the runner can download binaries through your proxy.
- If tests fail to compile, verify that all repositories implement the latest interface methods.


