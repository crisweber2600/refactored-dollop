# RAGStart

RAGStart provides a reference implementation of an event‑driven validation pipeline using .NET. The solution exposes reusable helpers under **ExampleLib** for validating and persisting entities with either Entity Framework Core or MongoDB.

## Table of Contents
1. [Quick Start](#quick-start)
2. [Repository Layout](#repository-layout)
3. [Core Components](#core-components)
4. [Configuring the Data Layer](#configuring-the-data-layer)
5. [Validation Workflow](#validation-workflow)
6. [Manual Validators](#manual-validators)
7. [External Flow Configuration](#external-flow-configuration)
8. [Running the Tests](#running-the-tests)
9. [Additional Guides](#additional-guides)
10. [Troubleshooting](#troubleshooting)

## Quick Start
1. Install the [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download).
2. Set `DOTNET_ROLL_FORWARD=Major` when running the solution on .NET 9 runtimes.
3. Execute `dotnet build` followed by `dotnet test --collect:"XPlat Code Coverage"`.
4. Reference `ExampleLib` from your own project and register services using the extension methods shown below.

## Repository Layout
- `src/ExampleLib` – domain models and infrastructure.
- `tests/ExampleLib.Tests` – unit tests for repositories and validators.
- `tests/ExampleLib.BDDTests` – Reqnroll scenarios demonstrating end‑to‑end behaviour.
- `features` – `.feature` files used by the BDD tests.
- `docs` – supplementary guides including `EFCoreReplicationGuide.md`.

## Core Components
`SaveAudit` stores the last metric and validation result for each entity instance:
```csharp
public class SaveAudit
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public decimal MetricValue { get; set; }
    public bool Validated { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
```
`ISummarisationPlanStore` provides access to `SummarisationPlan` objects describing how each entity is validated:
```csharp
public interface ISummarisationPlanStore
{
    SummarisationPlan<T> GetPlan<T>();
}
```
Repositories implement `IGenericRepository<T>` for EF Core and MongoDB. The EF variant updates the soft delete flag when deleting records:
```csharp
public interface IGenericRepository<T>
    where T : class, IValidatable, IBaseEntity, IRootEntity
{
    Task<T?> GetByIdAsync(int id, bool includeDeleted = false);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task DeleteAsync(T entity, bool hardDelete = false);
    Task<int> CountAsync();
}
```
MongoDB uses `MongoGenericRepository` with similar behaviour.

## Configuring the Data Layer
`SetupValidationBuilder` collects setup steps before applying them:
```csharp
public class SetupValidationBuilder
{
    private readonly List<Action<IServiceCollection>> _steps = new();
    private bool _useMongo;
    internal bool UsesMongo => _useMongo;
    public SetupValidationBuilder UseSqlServer<TContext>(string connectionString)
        where TContext : YourDbContext
    {
        _steps.Add(s => s.SetupDatabase<TContext>(connectionString));
        _useMongo = false;
        return this;
    }
    public SetupValidationBuilder UseMongo(string connectionString, string databaseName)
    {
        _steps.Add(s => s.SetupMongoDatabase(connectionString, databaseName));
        _useMongo = true;
        return this;
    }
    public IServiceCollection Apply(IServiceCollection services)
    {
        foreach (var step in _steps)
            step(services);
        return services;
    }
}
```
Combine setup and plan registration with `AddSetupValidation<T>`:
```csharp
public static IServiceCollection AddSetupValidation<T>(
    this IServiceCollection services,
    Action<SetupValidationBuilder> configure,
    Func<T, decimal>? metricSelector = null,
    ThresholdType thresholdType = ThresholdType.PercentChange,
    decimal thresholdValue = 0.1m)
{
    var builder = new SetupValidationBuilder();
    configure(builder);
    builder.Apply(services);

    services.AddSaveValidation<T>(metricSelector, thresholdType, thresholdValue);
    if (builder.UsesMongo)
        services.AddScoped<ISaveAuditRepository, MongoSaveAuditRepository>();
    else
        services.AddScoped<ISaveAuditRepository, EfSaveAuditRepository>();
    return services;
}
```
These helpers register the appropriate repositories and validation services for EF Core or MongoDB based on the builder configuration.

## Validation Workflow
Entities are validated against the registered `SummarisationPlan` each time a save occurs. The unit of work exposes `SaveChangesWithPlanAsync<TEntity>()` to automatically apply the plan when persisting data. Mongo collections use an interceptor to invoke the same logic whenever documents are inserted or updated.

## Manual Validators
`ManualValidatorService` stores simple predicate rules per type. Register the service and rules during startup:
```csharp
services.AddValidatorService()
        .AddValidatorRule<Order>(o => o.Total > 0);
```
The service evaluates every rule for the specified type and returns `true` only when all pass.

## External Flow Configuration
Validation flows may be loaded from JSON:
```json
[
  {
    "Type": "ExampleData.YourEntity, ExampleData",
    "SaveValidation": true,
    "MetricProperty": "Id",
    "ThresholdType": "RawDifference",
    "ThresholdValue": 2
  }
]
```
Load and register the configuration at startup:
```csharp
var json = File.ReadAllText("flows.json");
var options = ValidationFlowOptions.Load(json);
services.AddValidationFlows(options);
```

## Running the Tests
Run all unit and BDD tests with code coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```
VS Code tasks under `.vscode/tasks.json` provide convenient shortcuts for validating specific scenarios.

## Additional Guides
- `docs/EFCoreReplicationGuide.md` explains how to replicate the EF Core setup in another project.
- `Implementation.md` discusses designing class libraries at different maturity levels.

## Troubleshooting
- Ensure `DOTNET_ROLL_FORWARD=Major` is set when using .NET 9 runtimes.
- Run `dotnet restore` if packages fail to resolve.
- Remove `bin` and `obj` folders after SDK upgrades to avoid stale builds.

