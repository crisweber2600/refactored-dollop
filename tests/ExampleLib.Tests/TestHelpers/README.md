# ExampleLib Test Setup Migration

## Overview
Test setup functionality has been moved out of the main `ExampleLibValidationBuilder` class and into the test project to keep production code clean and provide better testing utilities.

## What Was Moved

### New Test Helpers Location
- **Path**: `tests/ExampleLib.Tests/TestHelpers/`
- **Main Class**: `ExampleLibTestBuilder`
- **Extension Methods**: `ServiceCollectionTestExtensions`

### Key Features Moved to Test Project

1. **ExampleLibTestBuilder**
   - Test-specific configuration builder with fluent API
   - In-memory database setup with unique names per test
   - Common test entity configuration
   - Predefined validation rules for testing
   - Mock service integration helpers

2. **ServiceCollectionTestExtensions**
   - `AddExampleLibForTesting()` - Quick test setup with defaults
   - `AddTestDatabase()` - Easy in-memory database configuration

## Usage Examples

### Basic Test Setup
```csharp
[Fact]
public void MyTest()
{
    // Simple setup with defaults
    var provider = ExampleLibTestBuilder.Create()
        .WithTestDefaults("MyTestApp")
        .Build();
    
    var runner = provider.GetRequiredService<IValidationRunner>();
    // ... rest of test
}
```

### Advanced Test Configuration
```csharp
[Fact]
public void MyAdvancedTest()
{
    var provider = ExampleLibTestBuilder.Create()
        .WithInMemoryDatabase("my-test-db")
        .ConfigureExampleLib(config =>
        {
            config.WithApplicationName("AdvancedTest")
                  .UseEntityFramework()
                  .AddSummarisationPlan<MyEntity>(
                      e => e.Value,
                      ThresholdType.RawDifference,
                      10.0m)
                  .AddValidationRules<MyEntity>(
                      e => !string.IsNullOrWhiteSpace(e.Name),
                      e => e.Validated);
        })
        .Build();
    
    // ... test logic
}
```

### Using Extension Methods
```csharp
[Fact]
public void MyExtensionTest()
{
    var services = new ServiceCollection();
    services.AddExampleLibForTesting(builder =>
    {
        builder.WithTestDefaults("ExtensionTest")
               .WithTestEntities<TestEntity>();
    });
    
    var provider = services.BuildServiceProvider();
    // ... test logic
}
```

### Test Entity Integration
```csharp
[Fact]
public void MyEntityTest()
{
    var provider = ExampleLibTestBuilder.Create()
        .WithTestDefaults()
        .WithTestEntities<TestEntity>() // Configures plans and rules
        .Build();
    
    var validator = provider.GetRequiredService<IManualValidatorService>();
    
    var entity = new TestEntity { Name = "Test", Validated = true };
    Assert.True(validator.Validate(entity));
}
```

## Benefits

### For Production Code
- ? Cleaner `ExampleLibValidationBuilder` focused on production scenarios
- ? No test-specific code in main library
- ? Reduced complexity in production configuration

### For Tests
- ? Dedicated test utilities designed for testing scenarios
- ? Better separation of concerns
- ? More flexible and powerful test configuration options
- ? Easier to maintain and extend test setups
- ? Consistent test patterns across the project

## Migration Guide

### Before (using production builder)
```csharp
var services = new ServiceCollection();
services.AddExampleLibValidation(builder => 
{
    builder.UseEntityFramework();
    // Limited configuration options
});
```

### After (using test builder)
```csharp
var provider = ExampleLibTestBuilder.Create()
    .WithTestDefaults("MyTest")
    .WithTestEntities<MyEntity>()
    .ConfigureExampleLib(config => 
    {
        // Full fluent configuration available
        config.UseEntityFramework()
              .AddSummarisationPlan<MyEntity>(...)
              .AddValidationRules<MyEntity>(...);
    })
    .Build();
```

## Available Test Entities
- `TestEntity` - Basic test entity with common properties (Id, Name, Value, Validated)
- Can be extended with custom entities as needed

## Global Usings
The test project now includes a global using for `ExampleLib.Tests.TestHelpers`, so test helper classes are available in all test files without explicit using statements.