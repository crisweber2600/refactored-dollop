using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleLib.Tests.TestHelpers;

/// <summary>
/// Example tests demonstrating the usage of ExampleLibTestBuilder.
/// These tests show how the test setup has been moved out of the main library.
/// </summary>
public class ExampleLibTestBuilderDemoTests
{
    [Fact]
    public void TestBuilder_WithDefaults_CreatesValidConfiguration()
    {
        // Arrange & Act
        var provider = ExampleLibTestBuilder.Create()
            .WithTestDefaults("DemoApp")
            .Build();

        // Assert
        Assert.NotNull(provider.GetService<IValidationRunner>());
        Assert.NotNull(provider.GetService<ISummarisationPlanStore>());
        Assert.NotNull(provider.GetService<IValidationPlanStore>());
        
        var appProvider = provider.GetService<IApplicationNameProvider>();
        Assert.NotNull(appProvider);
        Assert.Equal("DemoApp", appProvider.ApplicationName);
    }

    [Fact]
    public void TestBuilder_WithValidationRules_ConfiguresValidation()
    {
        // Arrange & Act
        var provider = ExampleLibTestBuilder.Create()
            .WithTestDefaults()
            .AddValidationRules<TestEntity>(
                entity => entity.Validated,
                entity => !string.IsNullOrWhiteSpace(entity.Name))
            .Build();

        // Assert
        var validator = provider.GetService<IManualValidatorService>();
        Assert.NotNull(validator);

        // Test validation rules
        var validEntity = new TestEntity { Name = "Test", Value = 100, Validated = true };
        var invalidEntity1 = new TestEntity { Name = "Test", Value = 100, Validated = false };
        var invalidEntity2 = new TestEntity { Name = "", Value = 100, Validated = true };
        
        Assert.True(validator.Validate(validEntity));
        Assert.False(validator.Validate(invalidEntity1));
        Assert.False(validator.Validate(invalidEntity2));
    }

    [Fact]
    public void TestBuilder_WithCustomConfiguration_AllowsFlexibility()
    {
        // Arrange & Act
        var provider = ExampleLibTestBuilder.Create()
            .WithInMemoryDatabase("custom-test-db")
            .WithApplicationName("CustomTestApp")
            .UseEntityFramework()
            .AddSummarisationPlan<TestEntity>(entity => entity.Value, ThresholdType.RawDifference, 15.0m)
            .AddValidationPlan<TestEntity>(threshold: 20.0, ValidationStrategy.Average)
            .AddValidationRules<TestEntity>(
                entity => !string.IsNullOrWhiteSpace(entity.Name),
                entity => entity.Value > 0)
            .Build();

        // Assert
        var appProvider = provider.GetService<IApplicationNameProvider>();
        var validator = provider.GetService<IManualValidatorService>();
        var planStore = provider.GetService<ISummarisationPlanStore>();
        var validationStore = provider.GetService<IValidationPlanStore>();

        Assert.NotNull(appProvider);
        Assert.NotNull(validator);
        Assert.NotNull(planStore);
        Assert.NotNull(validationStore);
        Assert.Equal("CustomTestApp", appProvider.ApplicationName);

        // Test that plans were configured
        var summaryPlan = planStore.GetPlan<TestEntity>();
        var validationPlan = validationStore.GetPlan<TestEntity>();
        
        Assert.NotNull(summaryPlan);
        Assert.NotNull(validationPlan);
        Assert.Equal(15.0m, summaryPlan.ThresholdValue);
        Assert.Equal(20.0, validationPlan.Threshold);

        // Test validation rules
        var validEntity = new TestEntity { Name = "Test", Value = 100, Validated = true };
        var invalidEntity = new TestEntity { Name = "", Value = -10, Validated = true };
        
        Assert.True(validator.Validate(validEntity));
        Assert.False(validator.Validate(invalidEntity));
    }

    [Fact]
    public void ServiceCollection_Extension_WithTestDefaults_Works()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddExampleLibForTesting();
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IValidationRunner>());
        Assert.NotNull(provider.GetService<ISummarisationPlanStore>());
        Assert.NotNull(provider.GetService<IValidationPlanStore>());
    }

    [Fact]
    public void ServiceCollection_Extension_WithCustomBuilder_Works()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddExampleLibForTesting("ExtensionTestApp");
        var provider = services.BuildServiceProvider();

        // Assert
        var appProvider = provider.GetService<IApplicationNameProvider>();
        
        Assert.NotNull(appProvider);
        Assert.Equal("ExtensionTestApp", appProvider.ApplicationName);
    }

    [Fact]
    public async Task TestBuilder_Integration_ValidatesCorrectly()
    {
        // Arrange
        var provider = ExampleLibTestBuilder.Create()
            .WithTestDefaults("IntegrationTest")
            .WithTestEntities<TestEntity>() // This adds default plans and rules
            .Build();

        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act
        var validEntity = new TestEntity { Name = "Valid", Value = 100, Validated = true };
        var invalidEntity = new TestEntity { Name = "", Value = 50, Validated = false };

        var validResult = await runner.ValidateAsync(validEntity);
        var invalidResult = await runner.ValidateAsync(invalidEntity);

        // Assert
        Assert.True(validResult);
        Assert.False(invalidResult);
    }

    [Fact]
    public void TestBuilder_WithMongo_ConfiguresCorrectly()
    {
        // Arrange & Act
        var provider = ExampleLibTestBuilder.Create()
            .WithTestDefaults("MongoTest")
            .UseMongo()
            .Build();

        // Assert
        var repository = provider.GetService<ISaveAuditRepository>();
        Assert.NotNull(repository);
        Assert.IsType<MongoSaveAuditRepository>(repository);
    }

    [Fact]
    public void TestBuilder_WithEntityFramework_ConfiguresCorrectly()
    {
        // Arrange & Act
        var provider = ExampleLibTestBuilder.Create()
            .WithTestDefaults("EfTest")
            .UseEntityFramework()
            .Build();

        // Assert
        var repository = provider.GetService<ISaveAuditRepository>();
        Assert.NotNull(repository);
        Assert.IsType<EfSaveAuditRepository>(repository);
    }

    [Fact]
    public void TestBuilder_WithTestEntities_ConfiguresProperly()
    {
        // Arrange & Act
        var provider = ExampleLibTestBuilder.Create()
            .WithTestDefaults()
            .WithTestEntities<TestEntity>()
            .Build();

        // Assert
        var planStore = provider.GetService<ISummarisationPlanStore>();
        var validationStore = provider.GetService<IValidationPlanStore>();
        var validator = provider.GetService<IManualValidatorService>();

        Assert.NotNull(planStore);
        Assert.NotNull(validationStore);
        Assert.NotNull(validator);

        // Test that plans were configured
        var summaryPlan = planStore.GetPlan<TestEntity>();
        var validationPlan = validationStore.GetPlan<TestEntity>();

        Assert.NotNull(summaryPlan);
        Assert.NotNull(validationPlan);
        Assert.Equal(5.0m, summaryPlan.ThresholdValue);
        Assert.Equal(10.0, validationPlan.Threshold);

        // Test validation rules
        var validEntity = new TestEntity { Name = "Test", Value = 100, Validated = true };
        var invalidEntity = new TestEntity { Name = "Test", Value = 100, Validated = false };
        
        Assert.True(validator.Validate(validEntity));
        Assert.False(validator.Validate(invalidEntity));
    }
}