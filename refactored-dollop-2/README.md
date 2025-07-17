# RAGStart

RAGStart is a .NET 9 reference solution for event-driven validation pipelines, supporting both Entity Framework Core and MongoDB. It demonstrates how to structure data access, validation rules, and audit tracking for robust, testable applications.

## Getting Started

1. **Install .NET 9 SDK**: [Download here](https://dotnet.microsoft.com/en-us/download)
2. **Set environment variable**: `DOTNET_ROLL_FORWARD=Major` (required for .NET 9)
3. **Build and test**:
   ```sh
   dotnet build
   dotnet test --collect:"XPlat Code Coverage"
   ```
4. **Reference ExampleLib** in your project and register services as shown below.

## Repository Structure
- `src/ExampleLib` – Domain models and infrastructure
- `tests/ExampleLib.Tests` – Unit tests for repositories and validators
- `tests/ExampleLib.Tests/ExampleData` – Sample entities and configuration
- `docs/` – Additional guides (e.g., `EFCoreReplicationGuide.md`)
- `WorkerService1` – Example .NET Worker Service project

## Core Concepts

### 1. Data Access & Auditing
- **IGenericRepository<T>**: Unified CRUD interface for EF Core and MongoDB
- **SaveAudit**: Tracks each save, including batch size, metric value, timestamp, and application name
- **Batch Operations**: Use `AddManyAsync` and `UpdateManyAsync` for efficiency

### 2. Validation Pipeline
- **Summarisation Validators**: Check metrics against configurable thresholds
- **Manual Validators**: Register custom predicates for business rules
- **Sequence Validators**: Compare per-key historical values to prevent cross-entity interference
- **ValidationRunner**: Orchestrates all validation checks

### 3. Configuration
- **Fluent API**: Type-safe, chainable configuration
- **JSON/Config File**: Load rules from configuration
- **EntityId Providers**: Use custom keys (e.g., Name, Code) for audit and validation

## Example Usage

### Registering Services
```csharp
// Entity Framework Core
services.AddExampleLib(builder =>
{
    builder.UseEf<TestDbContext>(options => options.UseSqlServer("<connection>"));
    builder.WithApplicationName("MyApp");
    builder.AddSummarisationPlan<Order>(o => o.Total, ThresholdType.RawDifference, 10m);
    builder.AddValidationRules<Order>(o => o.Total > 0, o => o.Status == "Open");
});

// MongoDB
services.AddMongoRepositories(options => options.ConnectionString = "mongodb://localhost");
```

### Entity Requirements
Entities must implement:
```csharp
public interface IValidatable { bool Validated { get; set; } }
public interface IBaseEntity { int Id { get; set; } }
public interface IRootEntity { }
```

### Custom EntityId Provider
```csharp
services.AddConfigurableEntityIdProvider(provider =>
{
    provider.RegisterSelector<SampleEntity>(e => e.Name);
    provider.RegisterSelector<OtherEntity>(e => e.Code);
});
```

### Using ValidationRunner
```csharp
var runner = provider.GetRequiredService<IValidationRunner>();
var order = new Order { Id = 1, Total = 5, Status = "Open", Validated = true };
bool ok = await runner.ValidateAsync(order); // true if all rules pass
```

### Sequence Validation
```csharp
var ok = SequenceValidator.Validate(items, x => x.Server, x => x.Value, (cur, prev) => Math.Abs(cur - prev) < 10);
```

## Worker Service Example
If using a .NET Worker Service, implement background tasks by inheriting from `BackgroundService` and inject your repositories and validators as needed.

## Testing
- Run all tests: `dotnet test --collect:"XPlat Code Coverage"`
- Coverage reports appear in `TestResults/`
- MongoDB tests use Mongo2Go (no external dependencies)

## Troubleshooting
- Set `DOTNET_ROLL_FORWARD=Major` for .NET 9
- Run `dotnet restore` if packages fail
- Delete `bin`/`obj` after SDK upgrades
- Register `AddValidationRunner` after validation services
- Ensure application name matches between services for correct audit history

## More Documentation
- See `docs/EFCoreReplicationGuide.md` and `docs/Implementation.md` for advanced topics

---
RAGStart provides a clear, extensible foundation for event-driven validation and audit in .NET 9 applications, with full test coverage and support for both EF Core and MongoDB.