# RAGStart

RAGStart showcases an event-driven validation pipeline built with .NET. The project exposes a small library called **ExampleLib** which demonstrates persisting and validating entities using either Entity Framework Core or MongoDB. The solution is intended as a reference for structuring data access, validation rules and test automation.

## Table of Contents
1. [Getting Started](#getting-started)
2. [Repository Structure](#repository-structure)
3. [Key Building Blocks](#key-building-blocks)
4. [Configuring the Data Layer](#configuring-the-data-layer)
5. [Validation Pipeline](#validation-pipeline)
6. [Manual Validation Service](#manual-validation-service)
7. [Using ValidationRunner](#using-validationrunner)
8. [Validating Sequences](#validating-sequences)
9. [Loading Validation Flows](#loading-validation-flows)
10. [Validation Details](#validation-details)
11. [Running Tests](#running-tests)
12. [Why Choose RAGStart?](#why-choose-ragstart)
13. [More Documentation](#more-documentation)
14. [Troubleshooting](#troubleshooting)

## Getting Started
1. Install the [.NET&nbsp;9 SDK](https://dotnet.microsoft.com/en-us/download).
2. Set the environment variable `DOTNET_ROLL_FORWARD=Major` when using .NET 9 runtimes.
   On Linux you can export this in your shell profile so builds are consistent.
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
- `ExampleData` doubles as a reference implementation when wiring up the library.
- *(BDD tests have been removed for clarity.)*
- `docs` – additional guides including `EFCoreReplicationGuide.md` and `Implementation.md`.

## Key Building Blocks
`SaveAudit` records details of each save, including the size of bulk operations.
Entities participating in validation must implement `IValidatable`,
`IBaseEntity` and `IRootEntity` so the framework can obtain their identifier and
store audits correctly:
```csharp
public class SaveAudit
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public decimal MetricValue { get; set; }
    public int BatchSize { get; set; }
    public bool Validated { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
```
The `ApplicationName` property records which application performed the save. If multiple services share the database, provide a distinct name so audits are kept separate.

Retrieve the last audit for an entity and inspect the application name:
```csharp
var last = repo.GetLastAudit("Order", "42");
Console.WriteLine(last?.ApplicationName);
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
Replace `"connection"` with your actual connection string. For SQLite or PostgreSQL simply call the corresponding `Use*` method on the builder.
Switch to MongoDB by calling `UseMongo` instead. Both providers expose `AddBatchAudit` for summarising bulk saves.

### Application Name Provider
Audits are tagged with the application name to keep metrics separate. Register a provider at startup:
```csharp
services.AddSingleton<IApplicationNameProvider>(
    new StaticApplicationNameProvider("MyApp"));
```
`ValidationService` uses this provider so all `SaveAudit` entries store the current application name.

## Validation Pipeline
Entities are validated against a `SummarisationPlan` whenever they are saved. The Entity Framework unit of work exposes `SaveChangesWithPlanAsync<TEntity>()`, while the MongoDB implementation uses an interceptor to apply the same logic during inserts and updates.

You can register multiple plans if different thresholds are needed. Each plan captures the metric selector and allowed variance for a specific entity type.

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

`AddValidatorRule` requires entities to implement `IValidatable`, `IBaseEntity` and `IRootEntity` ensuring only valid domain models can be registered.

## Using ValidationRunner
Once registered, request `IValidationRunner` from the service provider and call `ValidateAsync`
before persisting changes. This integrates validation with any repository:
```csharp
var provider = services.BuildServiceProvider();
var runner = provider.GetRequiredService<IValidationRunner>();
var repo = new EfGenericRepository<Order>(provider.GetRequiredService<DbContext>());
var order = new Order { Id = 1, Total = 5, Status = "Open", Validated = true };
await repo.AddAsync(order);
await provider.GetRequiredService<DbContext>().SaveChangesAsync();
bool ok = await runner.ValidateAsync(order);
```
`ok` indicates whether both summarisation and manual checks succeeded.

`ValidateAsync` now derives the identifier from `order.Id`, so callers no longer
need to pass an explicit string. The example above shows the new simplified
call signature.

## Validating Sequences
`SequenceValidator` compares successive items in a sequence. Always supply a
*key selector* so the validator can maintain a per-key history. Each item is
compared with the last value for that discriminator key, ensuring metrics from
different sources don't interfere. Provide key and
value selectors with an optional comparison delegate:
```csharp
var ok = SequenceValidator.Validate(items, x => x.Server, x => x.Value,
    (cur, prev) => Math.Abs(cur - prev) < 10);
```
If the sequence returns to a previously seen key it will be compared with the
value recorded for that key. For example:
```csharp
var servers = new[] { "ServerA", "ServerB", "ServerC", "ServerA" };
var check = SequenceValidator.Validate(servers,
    s => s,
    s => s,
    (cur, prev) => cur == prev);
```
The last `ServerA` item is checked against the first `ServerA` entry rather than
`ServerC`. This key-based history ensures related items are validated together.
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

## Validation Details
Below is a deeper look at the validation system and how it integrates with the data layer.

### How each validation works
- **Summarisation validators** inspect metrics and use `ThresholdValidator` to determine if new values fall within the configured threshold.
- **Manual validators** are predicates registered via `ManualValidatorService` and run alongside summarisation checks.
- **Sequence validators** keep the last value per key so sequential data from different sources is compared correctly.

### Dependency injection
- Validators are added through `AddSaveValidation` or `AddValidationRunner` and resolved from the DI container.
- `IValidationRunner` and repository instances are requested through constructor injection in your services.

### Generic repository pattern
- All repositories implement `IGenericRepository<T>` to expose common CRUD operations.
- EF Core and Mongo variants share this interface so switching providers requires no code changes.

### MongoDB support
- Call `services.AddMongoRepositories` to register Mongo repositories and the validation interceptor.
- Mongo operations record `SaveAudit` entries and trigger validation on inserts and updates.

### EF Core support
- Call `services.AddEfCoreRepositories<TestDbContext>` to register EF repositories.
- `SaveChangesWithPlanAsync` ensures validations run before the EF transaction commits.

### Integrating existing repositories
Implement `IGenericRepository<T>` in your repositories to connect to the pipeline.
1. Keep your existing data access logic and wrap it with the interface methods.
2. Inject `IValidationRunner` so each operation can run the configured rules.
3. Register the repository along with `AddValidationRunner` during startup.

#### Example
```csharp
public class CustomOrderRepository : IGenericRepository<Order>
{
    private readonly MyDbContext _db;
    private readonly IValidationRunner _runner;

    public CustomOrderRepository(MyDbContext db, IValidationRunner runner)
    {
        _db = db;
        _runner = runner;
    }

    public async Task AddAsync(Order order)
    {
        await _runner.ValidateAsync(order);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
    }

    // implement the remaining CRUD methods...
}
```


### Saving audit data
- Each save creates a `SaveAudit` record containing batch size, metric value, timestamp and application name.
- Both providers call `AddBatchAudit` so audits are persisted automatically.

### Additional notes
- Validation flows can be loaded from JSON at startup.
- Audits store timestamps to track when saves occurred.
- Register `IApplicationNameProvider` to separate metrics per application.
- Use `AddManyAsync` and `UpdateManyAsync` for efficient bulk operations.
- MongoDB tests run under **Mongo2Go** to avoid external dependencies.
- Sequence validation prevents cross-server metric interference.
- `ThresholdValidator.IsWithinThreshold` provides consistent checks across rules.
- Entities must implement `IValidatable`, `IBaseEntity` and `IRootEntity`.
- ValidationRunner returns `true` only if all rules succeed.
- Unit tests maintain coverage above 80% and treat warnings as errors.
## Running Tests
Run the unit tests with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```
The generated coverage report appears under `TestResults` and should exceed 80%.
Open the `.cobertura.xml` file in Visual Studio Code with the C# extension to visualise line-by-line results.
For quick iterations run:
```bash
dotnet test --no-build --no-restore
```

If you change the EF models, create a new migration:
```bash
dotnet ef migrations add MyMigration -p tests/ExampleLib.Tests/ExampleLib.Tests.csproj \
    -s tests/ExampleLib.Tests/ExampleLib.Tests.csproj -o ExampleData/Migrations
```

## Why Choose RAGStart?
- Provides a working example of event-driven validation with EF Core and MongoDB.
- Shows how to separate validation rules from persistence logic.
- Offers a one-line `AddValidationRunner` registration for existing repositories.
- Includes helper methods such as `AddManyAsync`, `UpdateManyAsync` and `AddBatchAudit` for efficient bulk operations.
- Demonstrates how to register per-entity rules using `AddSaveValidation`.
- Features a clear folder structure that can be reused in other projects.
- Unit tests illustrate validation runner usage for quick reference.
- Save audits include the originating application name for multi-app scenarios.

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
- Register `AddValidationRunner` after configuring validation services to avoid missing service errors.
- If validation results seem off, ensure the application name provided at startup matches the name used when audits were recorded. Mismatched names cause incorrect history.


