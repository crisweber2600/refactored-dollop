# RAGStart

RAGStart is a .NET 9 reference solution for event-driven validation pipelines, supporting both Entity Framework Core and MongoDB. It demonstrates how to structure data access, validation rules, and audit tracking for robust, testable applications.

## Getting Started

1. **Install .NET 9 SDK**: [Download here](https://dotnet.microsoft.com/en-us/download)
2. **Set environment variable**: `DOTNET_ROLL_FORWARD=Major` (required for .NET 9)
3. **Build and test**:dotnet build
dotnet test --collect:"XPlat Code Coverage"4. **Reference ExampleLib** in your project and register services as shown below.

## Repository Structure
- `src/ExampleLib` – Domain models and infrastructure
- `tests/ExampleLib.Tests` – Unit tests for repositories and validators
- `tests/ExampleLib.Tests/ExampleData` – Sample entities and configuration
- `docs/` – Additional guides (e.g., `EFCoreReplicationGuide.md`)
- `WorkerService1` – Example .NET Worker Service project with modern configuration

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

## Configuration Examples

### Modern Fluent API (Recommended)services.ConfigureExampleLib(config =>
{
    config.WithApplicationName("MyApp")
          .UseEntityFramework()
          .WithDefaultThresholds(ThresholdType.RawDifference, 5.0m)
          .AddSummarisationPlan<Order>(
              entity => entity.Total,
              ThresholdType.RawDifference,
              10.0m)
          .AddValidationPlan<Order>(threshold: 25.0, ValidationStrategy.Average)
          .AddValidationRules<Order>(
              order => order.Total > 0,
              order => !string.IsNullOrWhiteSpace(order.Status),
              order => order.Validated)
          .WithConfigurableEntityIds(provider =>
          {
              provider.RegisterSelector<Order>(o => o.OrderNumber);
              provider.RegisterSelector<Customer>(c => c.Email);
          });
});
### MongoDB Configurationservices.ConfigureExampleLib(config =>
{
    config.WithApplicationName("MyApp")
          .UseMongoDb(mongo => mongo.DefaultDatabaseName = "MyDatabase")
          .WithDefaultThresholds(ThresholdType.PercentChange, 0.15m)
          .AddSummarisationPlan<Order>(entity => entity.Total)
          .AddValidationRules<Order>(order => order.Total > 0);
});
### Configuration from JSON/Config Files// In appsettings.json
{
  "ExampleLib": {
    "ApplicationName": "MyService",
    "UseMongoDb": false,
    "DefaultThresholdType": "RawDifference",
    "DefaultThresholdValue": 25.5,
    "EntityIdProvider": {
      "Type": "Reflection",
      "PropertyPriority": ["Name", "Code", "Title"]
    }
  }
}

// In Program.cs
services.ConfigureExampleLib(builder.Configuration);
### Simplified Setup (Alternative)// For basic scenarios with minimal configuration
services.AddExampleLibValidation("MyApp");

// Then configure individual entities
services.ConfigureValidation<Order>(config =>
{
    config.WithMetricValidation(o => o.Total, 10.0m, 25.0)
          .WithRules(o => o.Total > 0, o => o.Validated)
          .WithEntityIdSelector(o => o.OrderNumber);
});
## Entity Requirements
Entities must implement these interfaces:public interface IValidatable { bool Validated { get; set; } }
public interface IBaseEntity { int Id { get; set; } }
public interface IRootEntity { }

public class Order : IValidatable, IBaseEntity, IRootEntity
{
    public int Id { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public bool Validated { get; set; }
}
## Worker Service Integration

For .NET Worker Services, inherit from `BackgroundService` and inject your repositories:
public class OrderProcessingWorker : BackgroundService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IValidationRunner _validationRunner;
    private readonly ILogger<OrderProcessingWorker> _logger;

    public OrderProcessingWorker(
        IRepository<Order> orderRepository,
        IValidationRunner validationRunner,
        ILogger<OrderProcessingWorker> logger)
    {
        _orderRepository = orderRepository;
        _validationRunner = validationRunner;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var orders = await GetPendingOrders();
            
            foreach (var order in orders)
            {
                // ValidationRunner is automatically called by repository
                if (await _validationRunner.ValidateAsync(order))
                {
                    await _orderRepository.AddAsync(order);
                    _logger.LogInformation("Order {OrderNumber} processed successfully", order.OrderNumber);
                }
                else
                {
                    _logger.LogWarning("Order {OrderNumber} failed validation", order.OrderNumber);
                }
            }
            
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
## Advanced Features

### Sequence Validationvar items = GetDataStream();
bool valid = SequenceValidator.Validate(
    items,
    item => item.ServerId,      // Key selector
    item => item.MetricValue,   // Value selector
    (current, previous) => Math.Abs(current - previous) < 10);
### Custom Repository Integrationpublic class CustomOrderRepository : IRepository<Order>
{
    private readonly DbContext _context;
    private readonly IValidationRunner _validationRunner;

    public async Task AddAsync(Order order)
    {
        if (!await _validationRunner.ValidateAsync(order))
            throw new ValidationException("Order validation failed");
            
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
    }
}
## Testing
- Run all tests: `dotnet test --collect:"XPlat Code Coverage"`
- Coverage reports appear in `TestResults/`
- MongoDB tests use Mongo2Go (no external dependencies)
- Use `ExampleLibTestBuilder` for test setup

## Migration Guide

### From Legacy Configuration// Before: Manual service registration
services.AddSingleton<ISummarisationPlanStore, InMemorySummarisationPlanStore>();
services.AddSingleton<IValidationPlanStore, InMemoryValidationPlanStore>();
services.AddSingleton<IApplicationNameProvider>(new StaticApplicationNameProvider("MyApp"));
// ... 20+ more lines

// After: Single fluent call
services.ConfigureExampleLib(config =>
{
    config.WithApplicationName("MyApp")
          .UseEntityFramework()
          .AddSummarisationPlan<Order>(o => o.Total, ThresholdType.RawDifference, 10.0m)
          .AddValidationRules<Order>(o => o.Total > 0);
});
### From Simplified Configuration// Before: Separate calls
services.AddExampleLibValidation("MyApp");
services.ConfigureValidation<Order>(config => config.WithMetricValidation(...));

// After: Single fluent call
services.ConfigureExampleLib(config =>
{
    config.WithApplicationName("MyApp")
          .UseEntityFramework()
          .AddSummarisationPlan<Order>(...)
          .AddValidationRules<Order>(...);
});
## Troubleshooting
- Set `DOTNET_ROLL_FORWARD=Major` for .NET 9
- Run `dotnet restore` if packages fail
- Delete `bin`/`obj` after SDK upgrades
- Ensure `TheNannyDbContext` is registered for EF Core
- Register `IMongoClient` for MongoDB
- Ensure application name matches between services for correct audit history

## More Documentation
- See `docs/EFCoreReplicationGuide.md` and `docs/Implementation.md` for advanced topics
- Check `WorkerService1/Program.cs` for complete working example

---
RAGStart provides a clear, extensible foundation for event-driven validation and audit in .NET 9 applications, with full test coverage and support for both EF Core and MongoDB.