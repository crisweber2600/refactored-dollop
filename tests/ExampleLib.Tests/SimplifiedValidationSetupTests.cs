using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleLib.Tests;

/// <summary>
/// Tests for the new simplified validation setup approach.
/// </summary>
public class SimplifiedValidationSetupTests
{
    public class TestEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public bool Validated { get; set; }
    }

    public class OrderEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public bool Validated { get; set; }
    }

    [Fact]
    public void AddExampleLibValidation_SetsUpCoreInfrastructure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddExampleLibForTesting("SimplifiedTest");

        // Act
        var provider = services.BuildServiceProvider();

        // Assert - Core services should be registered
        Assert.NotNull(provider.GetService<ISummarisationPlanStore>());
        Assert.NotNull(provider.GetService<IValidationPlanStore>());
        Assert.NotNull(provider.GetService<IApplicationNameProvider>());
        Assert.NotNull(provider.GetService<IEntityIdProvider>());
        Assert.NotNull(provider.GetService<ISaveAuditRepository>());
        Assert.NotNull(provider.GetService<IValidationService>());
        Assert.NotNull(provider.GetService<IValidationRunner>());
    }

    [Fact]
    public void AddExampleLibValidation_WithApplicationName_SetsApplicationNameProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddExampleLibForTesting("MyTestApp");

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        var appProvider = provider.GetService<IApplicationNameProvider>();
        Assert.NotNull(appProvider);
        Assert.Equal("MyTestApp", appProvider.ApplicationName);
    }

    [Fact]
    public void ConfigureValidation_WithMetricValidation_RegistersPlans()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddExampleLibForTesting("ConfigTest");

        // Act
        services.ConfigureValidation<TestEntity>(config => config
            .WithMetricValidation(
                metricSelector: entity => entity.Value,
                summaryThreshold: 100m,
                sequenceThreshold: 20.0,
                thresholdType: ThresholdType.RawDifference,
                strategy: ValidationStrategy.Average));

        var provider = services.BuildServiceProvider();

        // Assert - Check summarisation plan
        var summaryStore = provider.GetService<ISummarisationPlanStore>();
        Assert.NotNull(summaryStore);
        Assert.True(summaryStore.HasPlan<TestEntity>());
        
        var summaryPlan = summaryStore.GetPlan<TestEntity>();
        Assert.NotNull(summaryPlan);
        Assert.Equal(ThresholdType.RawDifference, summaryPlan.ThresholdType);
        Assert.Equal(100m, summaryPlan.ThresholdValue);

        // Assert - Check validation plan
        var validationStore = provider.GetService<IValidationPlanStore>();
        Assert.NotNull(validationStore);
        Assert.True(validationStore.HasPlan<TestEntity>());
        
        var validationPlan = validationStore.GetPlan<TestEntity>();
        Assert.NotNull(validationPlan);
        Assert.Equal(20.0, validationPlan.Threshold);
        Assert.Equal(ValidationStrategy.Average, validationPlan.Strategy);
    }

    [Fact]
    public void ConfigureValidation_WithRules_RegistersValidationRules()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddExampleLibForTesting("RulesTest");

        // Act
        services.ConfigureValidation<TestEntity>(config => config
            .WithRules(
                entity => !string.IsNullOrEmpty(entity.Name),
                entity => entity.Value > 0,
                entity => entity.Validated));

        var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<IManualValidatorService>();
        Assert.NotNull(validator);

        // Test valid entity
        var validEntity = new TestEntity { Name = "Valid", Value = 100m, Validated = true };
        Assert.True(validator.Validate(validEntity));

        // Test invalid entities
        var invalidEntity1 = new TestEntity { Name = "", Value = 100m, Validated = true };
        var invalidEntity2 = new TestEntity { Name = "Valid", Value = -10m, Validated = true };
        var invalidEntity3 = new TestEntity { Name = "Valid", Value = 100m, Validated = false };

        Assert.False(validator.Validate(invalidEntity1));
        Assert.False(validator.Validate(invalidEntity2));
        Assert.False(validator.Validate(invalidEntity3));
    }

    [Fact]
    public void ConfigureValidation_WithEntityIdSelector_RegistersCustomSelector()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddExampleLibForTesting("EntityIdTest");

        // Act
        services.ConfigureValidation<TestEntity>(config => config
            .WithEntityIdSelector(entity => entity.Name));

        var provider = services.BuildServiceProvider();

        // Assert
        var entityIdProvider = provider.GetService<IEntityIdProvider>();
        Assert.NotNull(entityIdProvider);
        Assert.IsType<ConfigurableEntityIdProvider>(entityIdProvider);

        var testEntity = new TestEntity { Id = 1, Name = "TestName", Value = 100m };
        var entityId = entityIdProvider.GetEntityId(testEntity);
        Assert.Equal("TestName", entityId);
    }

    [Fact]
    public void ConfigureValidation_MultipleEntities_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddExampleLibForTesting("MultiEntityTest");

        // Act
        services.ConfigureValidation<TestEntity>(config => config
            .WithMetricValidation(
                metricSelector: entity => entity.Value,
                summaryThreshold: 50m,
                sequenceThreshold: 10.0));

        services.ConfigureValidation<OrderEntity>(config => config
            .WithMetricValidation(
                metricSelector: entity => entity.Total,
                summaryThreshold: 1000m,
                sequenceThreshold: 100.0,
                thresholdType: ThresholdType.PercentChange)
            .WithEntityIdSelector(entity => entity.OrderNumber));

        var provider = services.BuildServiceProvider();

        // Assert - TestEntity plans
        var summaryStore = provider.GetService<ISummarisationPlanStore>();
        Assert.NotNull(summaryStore);
        Assert.True(summaryStore.HasPlan<TestEntity>());
        
        var testSummaryPlan = summaryStore.GetPlan<TestEntity>();
        Assert.NotNull(testSummaryPlan);
        Assert.Equal(50m, testSummaryPlan.ThresholdValue);

        // Assert - OrderEntity plans
        Assert.True(summaryStore.HasPlan<OrderEntity>());
        
        var orderSummaryPlan = summaryStore.GetPlan<OrderEntity>();
        Assert.NotNull(orderSummaryPlan);
        Assert.Equal(1000m, orderSummaryPlan.ThresholdValue);
        Assert.Equal(ThresholdType.PercentChange, orderSummaryPlan.ThresholdType);

        // Assert - Validation plans
        var validationStore = provider.GetService<IValidationPlanStore>();
        Assert.NotNull(validationStore);
        Assert.True(validationStore.HasPlan<TestEntity>());
        Assert.True(validationStore.HasPlan<OrderEntity>());

        var testValidationPlan = validationStore.GetPlan<TestEntity>();
        var orderValidationPlan = validationStore.GetPlan<OrderEntity>();
        Assert.NotNull(testValidationPlan);
        Assert.NotNull(orderValidationPlan);
        Assert.Equal(10.0, testValidationPlan.Threshold);
        Assert.Equal(100.0, orderValidationPlan.Threshold);
    }

    [Fact]
    public void ConfigureValidation_FluentChaining_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddExampleLibForTesting("FluentTest");

        // Act - Test fluent chaining
        services.ConfigureValidation<TestEntity>(config => config
            .WithMetricValidation(
                metricSelector: entity => entity.Value,
                summaryThreshold: 25m,
                sequenceThreshold: 5.0)
            .WithRules(entity => entity.Validated)
            .WithEntityIdSelector(entity => entity.Name));

        var provider = services.BuildServiceProvider();

        // Assert - All configurations should be applied
        var summaryStore = provider.GetService<ISummarisationPlanStore>();
        var validationStore = provider.GetService<IValidationPlanStore>();
        var validator = provider.GetService<IManualValidatorService>();
        var entityIdProvider = provider.GetService<IEntityIdProvider>();

        Assert.NotNull(summaryStore);
        Assert.NotNull(validationStore);
        Assert.NotNull(validator);
        Assert.NotNull(entityIdProvider);

        // Test summarisation plan
        var summaryPlan = summaryStore.GetPlan<TestEntity>();
        Assert.NotNull(summaryPlan);
        Assert.Equal(25m, summaryPlan.ThresholdValue);

        // Test validation plan
        var validationPlan = validationStore.GetPlan<TestEntity>();
        Assert.NotNull(validationPlan);
        Assert.Equal(5.0, validationPlan.Threshold);

        // Test rules
        var validEntity = new TestEntity { Name = "Test", Value = 100m, Validated = true };
        var invalidEntity = new TestEntity { Name = "Test", Value = 100m, Validated = false };
        Assert.True(validator.Validate(validEntity));
        Assert.False(validator.Validate(invalidEntity));

        // Test entity ID selector
        var entityId = entityIdProvider.GetEntityId(validEntity);
        Assert.Equal("Test", entityId);
    }

    [Fact]
    public void SimplifiedApproach_ComparedToOldApproach_ReducesComplexity()
    {
        // This test demonstrates the complexity reduction
        
        // OLD APPROACH (commented out for comparison):
        /*
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("TestApp")
                  .UseEntityFramework()
                  .WithConfigurableEntityIds(provider =>
                  {
                      provider.RegisterSelector<TestEntity>(entity => entity.Name);
                  })
                  .AddSummarisationPlan<TestEntity>(
                      entity => entity.Value,
                      ThresholdType.RawDifference,
                      100m)
                  .AddValidationPlan<TestEntity>(threshold: 20.0, ValidationStrategy.Average)
                  .AddValidationRules<TestEntity>(
                      entity => !string.IsNullOrEmpty(entity.Name),
                      entity => entity.Value > 0,
                      entity => entity.Validated);
        });
        */

        // NEW APPROACH:
        var services = new ServiceCollection();
        services.AddExampleLibForTesting("TestApp");

        services.ConfigureValidation<TestEntity>(config => config
            .WithMetricValidation(
                metricSelector: entity => entity.Value,
                summaryThreshold: 100m,
                sequenceThreshold: 20.0,
                thresholdType: ThresholdType.RawDifference,
                strategy: ValidationStrategy.Average)
            .WithRules(
                entity => !string.IsNullOrEmpty(entity.Name),
                entity => entity.Value > 0,
                entity => entity.Validated)
            .WithEntityIdSelector(entity => entity.Name));

        var provider = services.BuildServiceProvider();

        // Assert - Same functionality with less complexity
        Assert.NotNull(provider.GetService<IValidationRunner>());
        Assert.NotNull(provider.GetService<ISummarisationPlanStore>());
        Assert.NotNull(provider.GetService<IValidationPlanStore>());
        Assert.NotNull(provider.GetService<IManualValidatorService>());
        Assert.NotNull(provider.GetService<IEntityIdProvider>());
        
        // The new approach achieves the same result with:
        // - 2 method calls instead of 1 large fluent chain
        // - Automatic infrastructure setup
        // - Grouped related concerns (metric validation handles both plans)
        // - Cleaner separation of core setup vs entity-specific setup
    }
}