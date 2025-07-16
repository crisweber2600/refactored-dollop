# ExampleLib - Validation and Audit Library

ExampleLib is a comprehensive validation and audit library for .NET applications that provides automatic entity validation, audit trails, and sequence validation capabilities.

## Quick Start

### 1. Install the Package

```bash
dotnet add package ExampleLib
```

### 2. Basic Setup

The simplest way to configure ExampleLib is using the fluent configuration API:

```csharp
using ExampleLib.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// Configure ExampleLib with fluent API
builder.Services.ConfigureExampleLib(config =>
{
    config.WithApplicationName("MyApplication")
          .UseEntityFramework()  // or .UseMongoDb()
          .WithReflectionBasedEntityIds()
          .WithDefaultThresholds(ThresholdType.RawDifference, 5.0m);
});

// Register your entity repositories
builder.Services.AddEfRepository<MyEntity, MyDbContext>();

var host = builder.Build();
```

### 3. Define Your Entities

Entities must implement the required interfaces:

```csharp
public class MyEntity : IValidatable, IBaseEntity, IRootEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool Validated { get; set; }
}
```

### 4. Use the Repository

The repository automatically handles validation:

```csharp
public class MyService
{
    private readonly IRepository<MyEntity> _repository;

    public MyService(IRepository<MyEntity> repository)
    {
        _repository = repository;
    }

    public async Task CreateEntityAsync(string name, decimal amount)
    {
        var entity = new MyEntity { Name = name, Amount = amount };
        
        // Validation happens automatically in AddAsync
        await _repository.AddAsync(entity);
    }
}
```

## Configuration Options

### Fluent Configuration

ExampleLib provides a fluent configuration API for easy setup:

```csharp
builder.Services.ConfigureExampleLib(config =>
{
    config
        // Basic configuration
        .WithApplicationName("MyApp")
        .WithDefaultThresholds(ThresholdType.PercentChange, 0.1m)
        
        // Choose data store
        .UseEntityFramework()  // or .UseMongoDb()
        
        // Configure entity ID extraction
        .WithReflectionBasedEntityIds("Name", "Code", "Title")
        // or .WithConfigurableEntityIds(provider => {...})
        
        // Add validation plans
        .AddSummarisationPlan<MyEntity>(
            entity => entity.Amount,
            ThresholdType.RawDifference,
            5.0m)
        
        .AddValidationPlan<MyEntity>(threshold: 10.0)
        
        // Add manual validation rules
        .AddValidationRules<MyEntity>(
            entity => !string.IsNullOrWhiteSpace(entity.Name),
            entity => entity.Amount > 0)
        
        // Register repositories
        .AddEntityFrameworkRepositories<MyDbContext>();
});
```

### Configuration File

You can also configure ExampleLib using appsettings.json:

```json
{
  "ExampleLib": {
    "ApplicationName": "MyApplication",
    "UseMongoDb": false,
    "DefaultThresholdType": "RawDifference",
    "DefaultThresholdValue": 5.0,
    "EntityIdProvider": {
      "Type": "Reflection",
      "PropertyPriority": ["Name", "Code", "Title"]
    },
    "MongoDb": {
      "DefaultDatabaseName": "MyAppDb"
    }
  }
}
```

Then configure in code:

```csharp
builder.Services.ConfigureExampleLib(builder.Configuration);
```

### Advanced Configuration

For maximum control, you can configure services manually:

```csharp
// Core validation services
builder.Services.AddExampleLibValidation();

// Configure stores and plans manually
var summarisationStore = new InMemorySummarisationPlanStore();
var validationStore = new InMemoryValidationPlanStore();

summarisationStore.AddPlan(new SummarisationPlan<MyEntity>(
    entity => entity.Amount,
    ThresholdType.RawDifference,
    5.0m));

validationStore.AddPlan<MyEntity>(new ValidationPlan(
    typeof(MyEntity), 
    threshold: 10.0, 
    ValidationStrategy.Count));

builder.Services.AddSingleton<ISummarisationPlanStore>(summarisationStore);
builder.Services.AddSingleton<IValidationPlanStore>(validationStore);

// Configure entity ID provider
builder.Services.AddConfigurableEntityIdProvider(provider =>
{
    provider.RegisterSelector<MyEntity>(entity => entity.Name);
});

// Register repositories
builder.Services.AddEfRepository<MyEntity, MyDbContext>();
```

## Entity ID Providers

ExampleLib supports different strategies for extracting entity identifiers for audit and validation:

### 1. Reflection-Based (Recommended)

Automatically discovers string properties that can serve as natural identifiers:

```csharp
.WithReflectionBasedEntityIds("Name", "Code", "Key", "Title")
```

### 2. Configurable

Allows manual configuration of selectors for each entity type:

```csharp
.WithConfigurableEntityIds(provider =>
{
    provider.RegisterSelector<Customer>(c => c.CustomerCode);
    provider.RegisterSelector<Order>(o => o.OrderNumber);
})
```

### 3. Default

Uses the entity's `Id.ToString()` as the identifier.

## Validation Types

ExampleLib provides three types of validation:

### 1. Manual Validation

Simple predicate-based rules:

```csharp
.AddValidationRules<MyEntity>(
    entity => !string.IsNullOrWhiteSpace(entity.Name),
    entity => entity.Amount > 0,
    entity => entity.Validated)
```

### 2. Summarisation Validation

Compares metric values against configured thresholds:

```csharp
.AddSummarisationPlan<MyEntity>(
    entity => entity.Amount,           // Metric selector
    ThresholdType.RawDifference,       // Comparison type
    5.0m)                              // Threshold value
```

### 3. Sequence Validation

Validates against historical audit data using ValidationPlan:

```csharp
.AddValidationPlan<MyEntity>(
    threshold: 10.0,                   // Maximum allowed difference
    ValidationStrategy.Count)          // Validation strategy
```

## Repository Patterns

### Entity Framework

```csharp
// Register for all entity types in a context
.AddEntityFrameworkRepositories<MyDbContext>()

// Or register individual repositories
builder.Services.AddEfRepository<MyEntity, MyDbContext>();
```

### MongoDB

```csharp
// Register for all entity types with default naming
.AddMongoRepositories("MyDatabase")

// Or register individual repositories
builder.Services.AddMongoRepository<MyEntity>("MyDatabase", "MyEntities");
```

### Custom Repository

You can also implement `IRepository<T>` directly:

```csharp
public class CustomRepository<T> : IRepository<T> 
    where T : class, IValidatable, IBaseEntity, IRootEntity
{
    private readonly IValidationRunner _validationRunner;
    
    public CustomRepository(IValidationRunner validationRunner)
    {
        _validationRunner = validationRunner;
    }
    
    public async Task AddAsync(T entity)
    {
        var isValid = await _validationRunner.ValidateAsync(entity);
        if (!isValid)
            throw new InvalidOperationException("Validation failed");
            
        // Your persistence logic here
    }
    
    // Implement other methods...
}
```

## Advanced Features

### Audit Trails

ExampleLib automatically creates audit records for all validated entities:

```csharp
// Audit records are stored in SaveAudit table/collection
public class SaveAudit
{
    public string EntityType { get; set; }    // "MyEntity"
    public string EntityId { get; set; }      // From EntityIdProvider
    public string ApplicationName { get; set; } // From configuration
    public decimal MetricValue { get; set; }   // From SummarisationPlan
    public bool Validated { get; set; }        // Validation result
    public DateTimeOffset Timestamp { get; set; }
}
```

### Sequence Validation Extensions

For advanced sequence validation scenarios:

```csharp
// Validate against audit history
var isValid = await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
    entities,
    auditDbContext.SaveAudits,
    entityIdProvider,
    validationPlan,
    entity => entity.Amount);
```

### Threshold Types

- **RawDifference**: Absolute difference between values
- **PercentChange**: Percentage change between values

### Validation Strategies

- **Count**: Count-based validation
- **Sum**: Sum-based validation
- **Average**: Average-based validation
- **Variance**: Variance-based validation

## Migration from Manual Configuration

If you're currently using manual service registration, you can easily migrate:

**Before:**
```csharp
// Old manual configuration
var summarisationPlanStore = new InMemorySummarisationPlanStore();
builder.Services.AddSingleton<ISummarisationPlanStore>(summarisationPlanStore);
summarisationPlanStore.AddPlan(new SummarisationPlan<MyEntity>(...));
// ... lots more setup code
```

**After:**
```csharp
// New fluent configuration
builder.Services.ConfigureExampleLib(config =>
{
    config.AddSummarisationPlan<MyEntity>(entity => entity.Amount, ThresholdType.RawDifference, 5.0m);
});
```

## Best Practices

1. **Use fluent configuration** for new projects - it's more maintainable and discoverable
2. **Choose appropriate EntityIdProvider** - reflection-based works well for most scenarios
3. **Configure validation thresholds** based on your business requirements
4. **Use configuration files** for environment-specific settings
5. **Implement proper error handling** for validation failures
6. **Monitor audit trails** for validation patterns and issues

## Troubleshooting

### Common Issues

1. **"No SummarisationPlan registered"** - Ensure you've added a summarisation plan for your entity type
2. **"Entity validation failed"** - Check your manual validation rules and threshold settings
3. **Missing audit records** - Verify your SaveAuditRepository is properly configured

### Enable Logging

ExampleLib integrates with standard .NET logging:

```csharp
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

For more detailed documentation and examples, visit the [ExampleLib GitHub repository](https://github.com/yourorg/examplelib).