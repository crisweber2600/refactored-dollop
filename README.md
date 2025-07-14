# RAGStart

RAGStart provides a reference implementation of an event‑driven validation pipeline using .NET. The solution exposes reusable helpers under **ExampleLib** for validating and persisting entities with either Entity Framework Core or MongoDB.

## Table of Contents
1. [Quick Start](#quick-start)
2. [Repository Layout](#repository-layout)
3. [Core Components](#core-components)
4. [Configuring the Data Layer](#configuring-the-data-layer)
5. [Validation Workflow](#validation-workflow)
6. [Manual Validators](#manual-validators)
7. [Sequence Validation](#sequence-validation)
8. [External Flow Configuration](#external-flow-configuration)
9. [Running the Tests](#running-the-tests)
10. [Additional Guides](#additional-guides)
11. [Troubleshooting](#troubleshooting)

## Quick Start
1. Install the [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download).
2. Set `DOTNET_ROLL_FORWARD=Major` when running the solution on .NET 9 runtimes.
3. Execute `dotnet build` followed by `dotnet test --collect:"XPlat Code Coverage"`.
4. Reference `ExampleLib` from your own project and register services using the extension methods shown below.
5. Use `AddManyAsync` for efficient seeding when populating test data.
6. Call `UpdateAsync` or `UpdateManyAsync` to modify records without exposing EF Core or MongoDB types.
7. Record bulk saves with `AddBatchAudit` so later validations know the previous batch size.
8. Leverage `SequenceValidator` for on-the-fly comparisons between successive records.
9. Use `SequenceValidator` with a `SummarisationPlan` when the same threshold logic should apply across sequences.

## Repository Layout
- `src/ExampleLib` – domain models and infrastructure.
- `tests/ExampleLib.Tests` – unit tests for repositories and validators.
- `tests/ExampleLib.BDDTests` – Reqnroll scenarios demonstrating end‑to‑end behaviour.
- `features` – `.feature` files used by the BDD tests.
- `features/RepositoryUpdate.feature` – verifies that updates are persisted correctly.
- `features/SequencePlan.feature` – shows how a summarisation plan works with the sequence validator.
- `docs` – supplementary guides including `EFCoreReplicationGuide.md`.

## Core Components
`SaveAudit` stores details about the most recent save for each entity. It now tracks the size of any batch operation as well:
```csharp
public class SaveAudit
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public decimal MetricValue { get; set; }
    // How many records were part of the operation
    public int BatchSize { get; set; }
    public bool Validated { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
```

### Batch Audits
`ISaveAuditRepository` also exposes helpers for summarising larger save operations. Use `AddBatchAudit` when persisting many records at once and later retrieve the most recent summary via `GetLastBatchAudit`.
```csharp
public interface ISaveAuditRepository
{
    SaveAudit? GetLastAudit(string entityType, string entityId);
    void AddAudit(SaveAudit audit);
    // New helpers
    SaveAudit? GetLastBatchAudit(string entityType);
    void AddBatchAudit(SaveAudit audit);
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
    /// <summary>
    /// Insert a collection of entities in one operation.
    /// </summary>
    Task AddManyAsync(IEnumerable<T> entities);
    /// <summary>
    /// Update a single entity already tracked.
    /// </summary>
    Task UpdateAsync(T entity);
    /// <summary>
    /// Apply updates to multiple entities at once.
    /// </summary>
    Task UpdateManyAsync(IEnumerable<T> entities);
    Task DeleteAsync(T entity, bool hardDelete = false);
    Task<int> CountAsync();
}
```
MongoDB uses `MongoGenericRepository` with similar behaviour.

### Deleting Entities
- Call `DeleteAsync(entity)` to perform a soft delete.
- Soft deletes mark the entity as unvalidated by setting `Validated` to false.
- Pass `hardDelete: true` to remove the record permanently.
- Deleted records can be retrieved with `includeDeleted: true` when using `GetByIdAsync`.
- Both repository implementations follow this pattern.

Use `AddManyAsync` when seeding data or performing bulk inserts:

```csharp
var items = new[] { new Foo { Id = 1 }, new Foo { Id = 2 } };
await repository.AddManyAsync(items);
```
After inserting a collection, create a `SaveAudit` with the total count and call `AddBatchAudit` to persist the batch summary.

To modify entities, use `UpdateAsync` or `UpdateManyAsync` before saving:

```csharp
foo.Name = "Updated";
await repository.UpdateAsync(foo);
await context.SaveChangesAsync();
```

### Sample Foo Entity
`Foo` lives only in the BDD test project and demonstrates a minimal entity used for repository scenarios.

```csharp
public class Foo : IValidatable, IBaseEntity, IRootEntity
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool Validated { get; set; }
}
```

The tests register a derived `TestDbContext` that exposes `DbSet<Foo> Foos` and configures the mapping via `OnModelCreating`.

```csharp
public class TestDbContext : YourDbContext
{
    public TestDbContext(DbContextOptions<YourDbContext> options) : base(options) { }
    public DbSet<Foo> Foos => Set<Foo>();
}
```

## Configuring the Data Layer
`SetupValidationBuilder` collects setup steps before applying them:
Both the EF Core and MongoDB repositories expose `AddManyAsync` so large lists can be inserted efficiently regardless of provider.
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
`ManualValidatorService` exposes a public `Rules` dictionary of predicates keyed by type.
Create and register the service during startup then add rules as needed:
```csharp
services.AddValidatorService()
        .AddValidatorRule<Order>(o => o.Total > 0);
```
The service evaluates every rule for the specified type and returns `true` only when all pass.

- `AddValidatorService` registers a single `ManualValidatorService` instance.
- Rules can be added for any type via `AddValidatorRule<T>()`.
- Access `ManualValidatorService.Rules` from DI to inspect existing predicates.
- Multiple rules for a type are evaluated sequentially.
- When no rules exist the service simply returns `true`.

## Sequence Validation
`SequenceValidator` provides a lightweight way to compare successive records using lambda expressions. Specify a key selector and value selector; an optional delegate lets you control how values are compared.
```csharp
var ok = SequenceValidator.Validate(
    items,
    x => x.Server,
    x => x.Value,
    (cur, prev) => Math.Abs(cur - prev) < 10);
```
If you omit the last delegate, equality comparison on the selected value is used instead. A summarisation plan can also drive the comparison logic:
```csharp
var plan = new SummarisationPlan<MyEntity>(e => e.Value, ThresholdType.RawDifference, 5);
bool passes = SequenceValidator.Validate(items, e => e.Server, plan);
```
The validator now checks every item against its immediate predecessor, ensuring
that sequences with a single key are validated correctly. Use a summarisation
plan whenever you need a consistent threshold across validations.

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
This helper wires up save, commit and delete validations according to the JSON
definition, reducing boilerplate service registrations.

## Running the Tests
Run all unit and BDD tests with code coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```
Tests are compiled with warnings treated as errors. Any build warning will fail
the run, so ensure packages resolve cleanly. The generated coverage report should
exceed 80% when all tests pass.
VS Code tasks under `.vscode/tasks.json` provide convenient shortcuts for validating specific scenarios.

## Additional Guides
- `docs/EFCoreReplicationGuide.md` explains how to replicate the EF Core setup in another project.
- `Implementation.md` discusses designing class libraries at different maturity levels.
- The new `RepositoryUpdate.feature` demonstrates updating entities via BDD tests.
- `SequencePlan.feature` shows plan-based sequence validation in action.

## Troubleshooting
- Ensure `DOTNET_ROLL_FORWARD=Major` is set when using .NET 9 runtimes.
- Run `dotnet restore` if packages fail to resolve.
- Remove `bin` and `obj` folders after SDK upgrades to avoid stale builds.
- MongoDB tests rely on **Mongo2Go**; ensure the runner can download binaries through your network proxy.
- If packages resolve to different preview versions, update your project references to avoid NU1603 warnings.
- If tests fail to compile, verify that all repositories implement the latest interface methods.
- Missing batch audit data usually means `AddBatchAudit` was not invoked after bulk saves.
- If results from `SequenceValidator` seem incorrect, verify the items are ordered as intended.
- When using a plan with `SequenceValidator`, ensure the threshold values match your expectations.

