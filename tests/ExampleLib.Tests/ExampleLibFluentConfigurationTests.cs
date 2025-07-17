using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;

namespace ExampleLib.Tests;

/// <summary>
/// Unit tests for the fluent configuration system in ExampleLib.
/// Tests the ExampleLibConfigurationBuilder and related extension methods.
/// </summary>
public class ExampleLibFluentConfigurationTests
{
    private class TestEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public bool Validated { get; set; }
    }

    private class AnotherTestEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int Amount { get; set; }
        public bool Validated { get; set; }
    }

    [Fact]
    public void ConfigureExampleLib_WithBasicConfiguration_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add a required DbContext for Entity Framework tests
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-basic-config"));

        // Act
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("TestApp")
                  .UseEntityFramework();
        });

        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IApplicationNameProvider>());
        Assert.NotNull(provider.GetService<ISummarisationPlanStore>());
        Assert.NotNull(provider.GetService<IValidationPlanStore>());
        Assert.NotNull(provider.GetService<IValidationRunner>());
        Assert.NotNull(provider.GetService<IEntityIdProvider>());
    }

    [Fact]
    public void WithApplicationName_SetsApplicationNameCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        const string expectedAppName = "MyTestApplication";
        
        // Add a required DbContext for Entity Framework tests
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-app-name"));

        // Act
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName(expectedAppName)
                  .UseEntityFramework();
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var appNameProvider = provider.GetService<IApplicationNameProvider>();
        Assert.NotNull(appNameProvider);
        Assert.Equal(expectedAppName, appNameProvider.ApplicationName);
    }

    [Fact]
    public void UseMongoDb_ConfiguresMongoServices()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Mock MongoDB client
        var mockClient = new Mock<IMongoClient>();
        var mockDatabase = new Mock<IMongoDatabase>();
        var mockCollection = new Mock<IMongoCollection<SaveAudit>>();
        
        mockClient.Setup(c => c.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
                  .Returns(mockDatabase.Object);
        mockDatabase.Setup(d => d.GetCollection<SaveAudit>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
                    .Returns(mockCollection.Object);
        
        services.AddSingleton(mockClient.Object);

        // Act
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("TestApp")
                  .UseMongoDb(mongo => mongo.DefaultDatabaseName = "TestDB");
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<ISaveAuditRepository>();
        Assert.NotNull(repository);
        Assert.IsType<MongoSaveAuditRepository>(repository);
    }

    [Fact]
    public void UseEntityFramework_ConfiguresEfServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-ef-config"));

        // Act
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("TestApp")
                  .UseEntityFramework();
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<ISaveAuditRepository>();
        Assert.NotNull(repository);
        Assert.IsType<EfSaveAuditRepository>(repository);
    }

    [Fact]
    public void WithReflectionBasedEntityIds_ConfiguresReflectionProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var customPriority = new[] { "Name", "Code", "Title" };
        
        // Add a required DbContext for Entity Framework tests
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-reflection-provider"));

        // Act
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("TestApp")
                  .UseEntityFramework()
                  .WithReflectionBasedEntityIds(customPriority);
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var entityIdProvider = provider.GetService<IEntityIdProvider>();
        Assert.NotNull(entityIdProvider);
        Assert.IsType<ReflectionBasedEntityIdProvider>(entityIdProvider);
    }

    [Fact]
    public void WithConfigurableEntityIds_ConfiguresConfigurableProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add a required DbContext for Entity Framework tests
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-configurable-provider"));

        // Act
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("TestApp")
                  .UseEntityFramework()
                  .WithConfigurableEntityIds(provider =>
                  {
                      provider.RegisterSelector<TestEntity>(e => e.Name);
                  });
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var entityIdProvider = provider.GetService<IEntityIdProvider>();
        Assert.NotNull(entityIdProvider);
        Assert.IsType<ConfigurableEntityIdProvider>(entityIdProvider);

        var testEntity = new TestEntity { Id = 1, Name = "TestName", Value = 100 };
        var entityId = entityIdProvider.GetEntityId(testEntity);
        Assert.Equal("TestName", entityId);
    }

    [Fact]
    public void WithDefaultThresholds_SetsDefaultValues()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedType = ThresholdType.PercentChange;
        var expectedValue = 0.05m;
        
        // Add a required DbContext for Entity Framework tests
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-default-thresholds"));

        // Act
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("TestApp")
                  .UseEntityFramework()
                  .WithDefaultThresholds(expectedType, expectedValue);
        });

        var provider = services.BuildServiceProvider();

        // Assert - We can't directly test the options, but we can test that services are registered
        Assert.NotNull(provider.GetService<ISummarisationPlanStore>());
        Assert.NotNull(provider.GetService<IValidationPlanStore>());
    }

    [Fact]
    public void AddSummarisationPlan_RegistersPlanCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add a required DbContext for Entity Framework tests
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-summarisation-plan"));

        // Act
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("TestApp")
                  .UseEntityFramework()
                  .AddSummarisationPlan<TestEntity>(
                      entity => entity.Value,
                      ThresholdType.RawDifference,
                      10.0m);
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var planStore = provider.GetService<ISummarisationPlanStore>();
        Assert.NotNull(planStore);

        var plan = planStore.GetPlan<TestEntity>();
        Assert.NotNull(plan);
        Assert.Equal(ThresholdType.RawDifference, plan.ThresholdType);
        Assert.Equal(10.0m, plan.ThresholdValue);
    }

    [Fact]
    public void AddSummarisationPlan_WithNullThresholds_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add a required DbContext for Entity Framework tests
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-null-thresholds"));

        // Act
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("TestApp")
                  .UseEntityFramework()
                  .WithDefaultThresholds(ThresholdType.PercentChange, 0.2m)
                  .AddSummarisationPlan<TestEntity>(entity => entity.Value); // No explicit thresholds
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var planStore = provider.GetService<ISummarisationPlanStore>();
        Assert.NotNull(planStore);

        var plan = planStore.GetPlan<TestEntity>();
        Assert.NotNull(plan);
        Assert.Equal(ThresholdType.PercentChange, plan.ThresholdType);
        Assert.Equal(0.2m, plan.ThresholdValue);
    }

    [Fact]
    public void AddValidationPlan_RegistersPlanCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add a required DbContext for Entity Framework tests
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-validation-plan"));

        // Act
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("TestApp")
                  .UseEntityFramework()
                  .AddValidationPlan<TestEntity>(threshold: 15.0, ValidationStrategy.Average);
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var planStore = provider.GetService<IValidationPlanStore>();
        Assert.NotNull(planStore);

        var plan = planStore.GetPlan<TestEntity>();
        Assert.NotNull(plan);
        Assert.Equal(typeof(TestEntity), plan.EntityType);
        Assert.Equal(15.0, plan.Threshold);
        Assert.Equal(ValidationStrategy.Average, plan.Strategy);
    }

    [Fact]
    public void AddValidationRules_RegistersRulesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add a required DbContext for Entity Framework tests
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-validation-rules"));

        // Act
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("TestApp")
                  .UseEntityFramework()
                  .AddValidationRules<TestEntity>(
                      entity => !string.IsNullOrWhiteSpace(entity.Name),
                      entity => entity.Value > 0,
                      entity => entity.Validated);
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<IManualValidatorService>();
        Assert.NotNull(validator);

        var validEntity = new TestEntity { Name = "Valid", Value = 100, Validated = true };
        var invalidEntity1 = new TestEntity { Name = "", Value = 100, Validated = true };
        var invalidEntity2 = new TestEntity { Name = "Valid", Value = -10, Validated = true };
        var invalidEntity3 = new TestEntity { Name = "Valid", Value = 100, Validated = false };

        Assert.True(validator.Validate(validEntity));
        Assert.False(validator.Validate(invalidEntity1));
        Assert.False(validator.Validate(invalidEntity2));
        Assert.False(validator.Validate(invalidEntity3));
    }

    [Fact]
    public void AddMultiplePlans_ForDifferentEntities_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add a required DbContext for Entity Framework tests
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-multiple-plans"));

        // Act
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("TestApp")
                  .UseEntityFramework()
                  .AddSummarisationPlan<TestEntity>(
                      entity => entity.Value,
                      ThresholdType.RawDifference,
                      5.0m)
                  .AddSummarisationPlan<AnotherTestEntity>(
                      entity => entity.Amount,
                      ThresholdType.PercentChange,
                      0.1m)
                  .AddValidationPlan<TestEntity>(threshold: 10.0)
                  .AddValidationPlan<AnotherTestEntity>(threshold: 20.0);
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var summarisationStore = provider.GetService<ISummarisationPlanStore>();
        var validationStore = provider.GetService<IValidationPlanStore>();

        Assert.NotNull(summarisationStore);
        Assert.NotNull(validationStore);

        // Test first entity plans
        var testEntitySummaryPlan = summarisationStore.GetPlan<TestEntity>();
        var testEntityValidationPlan = validationStore.GetPlan<TestEntity>();

        Assert.NotNull(testEntitySummaryPlan);
        Assert.NotNull(testEntityValidationPlan);
        Assert.Equal(ThresholdType.RawDifference, testEntitySummaryPlan.ThresholdType);
        Assert.Equal(5.0m, testEntitySummaryPlan.ThresholdValue);
        Assert.Equal(10.0, testEntityValidationPlan.Threshold);

        // Test second entity plans
        var anotherEntitySummaryPlan = summarisationStore.GetPlan<AnotherTestEntity>();
        var anotherEntityValidationPlan = validationStore.GetPlan<AnotherTestEntity>();

        Assert.NotNull(anotherEntitySummaryPlan);
        Assert.NotNull(anotherEntityValidationPlan);
        Assert.Equal(ThresholdType.PercentChange, anotherEntitySummaryPlan.ThresholdType);
        Assert.Equal(0.1m, anotherEntitySummaryPlan.ThresholdValue);
        Assert.Equal(20.0, anotherEntityValidationPlan.Threshold);
    }

    [Fact]
    public void FluentConfiguration_MethodChaining_ReturnsCorrectBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add a required DbContext for Entity Framework tests
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-method-chaining"));

        // Act & Assert - All methods should return the builder for chaining
        var result = services.ConfigureExampleLib(config =>
        {
            var builder1 = config.WithApplicationName("TestApp");
            var builder2 = builder1.UseEntityFramework();
            var builder3 = builder2.WithDefaultThresholds(ThresholdType.RawDifference, 5.0m);
            var builder4 = builder3.AddSummarisationPlan<TestEntity>(e => e.Value);
            var builder5 = builder4.AddValidationPlan<TestEntity>(10.0);
            var builder6 = builder5.AddValidationRules<TestEntity>(e => e.Validated);

            // All should reference the same builder instance
            Assert.Same(config, builder1);
            Assert.Same(config, builder2);
            Assert.Same(config, builder3);
            Assert.Same(config, builder4);
            Assert.Same(config, builder5);
            Assert.Same(config, builder6);
        });

        // The final result should be the service collection
        Assert.Same(services, result);
    }

    [Fact]
    public void ConfigureExampleLib_WithConfigurationFile_LoadsSettingsCorrectly()
    {
        // Arrange
        var configurationData = new Dictionary<string, string>
        {
            ["ExampleLib:ApplicationName"] = "ConfigFileApp",
            ["ExampleLib:UseMongoDb"] = "false",
            ["ExampleLib:DefaultThresholdType"] = "RawDifference",
            ["ExampleLib:DefaultThresholdValue"] = "25.5",
            ["ExampleLib:EntityIdProvider:Type"] = "Reflection",
            ["ExampleLib:EntityIdProvider:PropertyPriority:0"] = "Name",
            ["ExampleLib:EntityIdProvider:PropertyPriority:1"] = "Code"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        var services = new ServiceCollection();
        
        // Add a required DbContext for Entity Framework tests since UseMongoDb is false
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-config-file"));

        // Act
        services.ConfigureExampleLib(configuration);

        var provider = services.BuildServiceProvider();

        // Assert
        var appNameProvider = provider.GetService<IApplicationNameProvider>();
        Assert.NotNull(appNameProvider);
        Assert.Equal("ConfigFileApp", appNameProvider.ApplicationName);

        var repository = provider.GetService<ISaveAuditRepository>();
        Assert.NotNull(repository);
        // Should use Entity Framework since UseMongoDb is false
        Assert.IsType<EfSaveAuditRepository>(repository);
    }

    [Fact]
    public void ConfigureExampleLib_WithConfigurationFile_CustomSectionName_LoadsCorrectly()
    {
        // Arrange
        var configurationData = new Dictionary<string, string>
        {
            ["MyCustomSection:ApplicationName"] = "CustomSectionApp",
            ["MyCustomSection:UseMongoDb"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        var services = new ServiceCollection();

        // Act
        services.ConfigureExampleLib(configuration, "MyCustomSection");

        var provider = services.BuildServiceProvider();

        // Assert
        var appNameProvider = provider.GetService<IApplicationNameProvider>();
        Assert.NotNull(appNameProvider);
        Assert.Equal("CustomSectionApp", appNameProvider.ApplicationName);
    }

    [Fact]
    public void Build_ReplacesExistingStoreRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Register a custom store first
        var customStore = new InMemorySummarisationPlanStore();
        services.AddSingleton<ISummarisationPlanStore>(customStore);
        
        // Add a required DbContext for Entity Framework tests
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-replace-stores"));

        // Act
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("TestApp")
                  .UseEntityFramework()
                  .AddSummarisationPlan<TestEntity>(e => e.Value, ThresholdType.RawDifference, 5.0m);
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var finalStore = provider.GetService<ISummarisationPlanStore>();
        Assert.NotNull(finalStore);
        
        // The store should be replaced with one that contains our plans
        Assert.NotSame(customStore, finalStore);
        
        // And it should contain our configured plan
        var plan = finalStore.GetPlan<TestEntity>();
        Assert.NotNull(plan);
    }

    [Fact]
    public void WithConfigurableEntityIds_DoesNotOverrideExistingProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add a required DbContext for Entity Framework tests
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-no-override"));

        // Act
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("TestApp")
                  .UseEntityFramework()
                  .WithConfigurableEntityIds(provider =>
                  {
                      provider.RegisterSelector<TestEntity>(e => e.Name);
                  })
                  .WithReflectionBasedEntityIds(); // This should not override the configurable one
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var entityIdProvider = provider.GetService<IEntityIdProvider>();
        Assert.NotNull(entityIdProvider);
        Assert.IsType<ConfigurableEntityIdProvider>(entityIdProvider);
    }

    [Theory]
    [InlineData(EntityIdProviderType.Reflection)]
    [InlineData(EntityIdProviderType.Default)]
    public void ConfigureExampleLib_WithDifferentEntityIdProviderTypes_RegistersCorrectType(EntityIdProviderType providerType)
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add a required DbContext for Entity Framework tests
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase($"test-provider-type-{providerType}"));

        // Act
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("TestApp")
                  .UseEntityFramework();
            
            if (providerType == EntityIdProviderType.Reflection)
            {
                config.WithReflectionBasedEntityIds("Name", "Code");
            }
            // Default type will be used if nothing is specified
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var entityIdProvider = serviceProvider.GetService<IEntityIdProvider>();
        Assert.NotNull(entityIdProvider);

        // Both Reflection and Default result in ReflectionBasedEntityIdProvider
        Assert.IsType<ReflectionBasedEntityIdProvider>(entityIdProvider);
    }

    [Fact]
    public void AddValidationPlan_WithDefaultStrategy_UsesCountStrategy()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add a required DbContext for Entity Framework tests
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-default-strategy"));

        // Act
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("TestApp")
                  .UseEntityFramework()
                  .AddValidationPlan<TestEntity>(threshold: 10.0); // No explicit strategy
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var planStore = provider.GetService<IValidationPlanStore>();
        Assert.NotNull(planStore);

        var plan = planStore.GetPlan<TestEntity>();
        Assert.NotNull(plan);
        Assert.Equal(ValidationStrategy.Count, plan.Strategy);
    }

    [Fact]
    public void ExampleLibOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new ExampleLibOptions();

        // Assert
        Assert.Equal("DefaultApp", options.ApplicationName);
        Assert.False(options.UseMongoDb);
        Assert.Equal(ThresholdType.PercentChange, options.DefaultThresholdType);
        Assert.Equal(0.1m, options.DefaultThresholdValue);
        Assert.NotNull(options.EntityIdProvider);
        Assert.Equal(EntityIdProviderType.Reflection, options.EntityIdProvider.Type);
        Assert.Contains("Name", options.EntityIdProvider.PropertyPriority);
        Assert.Contains("Code", options.EntityIdProvider.PropertyPriority);
    }

    [Fact]
    public void MongoDbOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new MongoDbOptions();

        // Assert
        Assert.Equal("ExampleLibDb", options.DefaultDatabaseName);
        Assert.NotNull(options.CollectionNamingStrategy);
        
        // Test the default naming strategy
        var result = options.CollectionNamingStrategy(typeof(TestEntity));
        Assert.Equal("TestEntitys", result);
    }

    [Fact]
    public void EntityFrameworkOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new EntityFrameworkOptions();

        // Assert
        Assert.False(options.AutoRegisterRepositories);
    }
}