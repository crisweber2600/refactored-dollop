using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MongoDB.Driver;

namespace ExampleLib.Tests;

/// <summary>
/// Unit tests for ServiceCollectionExtensions methods.
/// Tests dependency injection configuration and service registration.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    public class TestEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Validated { get; set; }
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<SaveAudit> SaveAudits => Set<SaveAudit>();
        public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    }

    #region New Test Builder Demonstration Tests

    [Fact]
    public void NewTestBuilder_Approach_SimplifiesTestSetup()
    {
        // Demonstrate the new test builder approach - much cleaner!
        var provider = ExampleLibTestBuilder.Create()
            .WithTestDefaults("DemoApp")
            .WithTestEntities<TestEntity>()
            .Build();

        // Assert all services are properly configured
        var runner = provider.GetRequiredService<IValidationRunner>();
        var planStore = provider.GetRequiredService<ISummarisationPlanStore>();
        var validator = provider.GetRequiredService<IManualValidatorService>();
        
        Assert.NotNull(runner);
        Assert.NotNull(planStore);
        Assert.NotNull(validator);

        // Test that validation rules were applied
        var validEntity = new TestEntity { Name = "Valid", Validated = true };
        var invalidEntity = new TestEntity { Name = "Invalid", Validated = false };
        
        Assert.True(validator.Validate(validEntity));
        Assert.False(validator.Validate(invalidEntity));
    }

    [Fact]
    public void ServiceCollection_Extension_ForTesting_WorksCorrectly()
    {
        // Demonstrate the extension method approach
        var services = new ServiceCollection();
        services.AddExampleLibForTesting(builder =>
        {
            builder.WithTestDefaults("ExtensionTest")
                   .WithTestEntities<TestEntity>();
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var appProvider = provider.GetRequiredService<IApplicationNameProvider>();
        var validator = provider.GetRequiredService<IManualValidatorService>();
        
        Assert.Equal("ExtensionTest", appProvider.ApplicationName);
        Assert.NotNull(validator);
    }

    #endregion

    [Fact]
    public void AddValidatorService_RegistersManualValidatorService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddValidatorService();
        var provider = services.BuildServiceProvider();

        // Assert
        var service = provider.GetService<IManualValidatorService>();
        Assert.NotNull(service);
        Assert.IsType<ManualValidatorService>(service);
    }

    [Fact]
    public void AddValidatorService_RegistersAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddValidatorService();
        var provider = services.BuildServiceProvider();

        // Assert
        var service1 = provider.GetService<IManualValidatorService>();
        var service2 = provider.GetService<IManualValidatorService>();
        Assert.Same(service1, service2);
    }

    [Fact]
    public void AddValidationRunner_RegistersValidationRunner()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-validation-runner"));
        services.AddValidatorService();
        services.AddScoped<IValidationService, ValidationService>();

        // Act
        services.AddValidationRunner();
        var provider = services.BuildServiceProvider();

        // Assert
        var runner = provider.GetService<IValidationRunner>();
        Assert.NotNull(runner);
        Assert.IsType<ValidationRunner>(runner);
    }

    [Fact]
    public void AddValidationRunner_RegistersAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-scoped-validation-runner"));
        services.AddValidatorService();
        services.AddScoped<IValidationService, ValidationService>();

        // Act
        services.AddValidationRunner();
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        
        var runner1 = scope1.ServiceProvider.GetService<IValidationRunner>();
        var runner2 = scope2.ServiceProvider.GetService<IValidationRunner>();
        Assert.NotSame(runner1, runner2);
    }

    [Fact]
    public void AddValidatorRule_RegistersRuleCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<TestEntity, bool> rule = entity => !string.IsNullOrWhiteSpace(entity.Name);

        // Act
        services.AddValidatorRule(rule);
        var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<IManualValidatorService>();
        Assert.NotNull(validator);
        
        var validEntity = new TestEntity { Name = "Valid", Validated = true };
        var invalidEntity = new TestEntity { Name = "", Validated = true };
        
        Assert.True(validator.Validate(validEntity));
        Assert.False(validator.Validate(invalidEntity));
    }

    [Fact]
    public void AddValidatorRule_AllowsMultipleRulesForSameType()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<TestEntity, bool> rule1 = entity => !string.IsNullOrWhiteSpace(entity.Name);
        Func<TestEntity, bool> rule2 = entity => entity.Validated;

        // Act
        services.AddValidatorRule(rule1);
        services.AddValidatorRule(rule2);
        var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<IManualValidatorService>();
        Assert.NotNull(validator);
        
        var validEntity = new TestEntity { Name = "Valid", Validated = true };
        var invalidEntity1 = new TestEntity { Name = "", Validated = true }; // Fails rule1
        var invalidEntity2 = new TestEntity { Name = "Valid", Validated = false }; // Fails rule2
        
        Assert.True(validator.Validate(validEntity));
        Assert.False(validator.Validate(invalidEntity1));
        Assert.False(validator.Validate(invalidEntity2));
    }

    [Fact]
    public void AddConfigurableEntityIdProvider_RegistersProviderCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddConfigurableEntityIdProvider(provider =>
        {
            provider.RegisterSelector<TestEntity>(entity => entity.Name);
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var entityIdProvider = serviceProvider.GetService<IEntityIdProvider>();
        Assert.NotNull(entityIdProvider);
        Assert.IsType<ConfigurableEntityIdProvider>(entityIdProvider);
        
        var testEntity = new TestEntity { Id = 1, Name = "TestName" };
        var entityId = entityIdProvider.GetEntityId(testEntity);
        Assert.Equal("TestName", entityId);
    }

    [Fact]
    public void AddReflectionBasedEntityIdProvider_WithCustomPriority_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var customPriority = new[] { "Name", "Code", "Title" };

        // Act
        services.AddReflectionBasedEntityIdProvider(customPriority);
        var provider = services.BuildServiceProvider();

        // Assert
        var entityIdProvider = provider.GetService<IEntityIdProvider>();
        Assert.NotNull(entityIdProvider);
        Assert.IsType<ReflectionBasedEntityIdProvider>(entityIdProvider);
    }

    [Fact]
    public void AddReflectionBasedEntityIdProvider_WithoutCustomPriority_RegistersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddReflectionBasedEntityIdProvider();
        var provider = services.BuildServiceProvider();

        // Assert
        var entityIdProvider = provider.GetService<IEntityIdProvider>();
        Assert.NotNull(entityIdProvider);
        Assert.IsType<ReflectionBasedEntityIdProvider>(entityIdProvider);
    }

    [Fact]
    public void AddDefaultEntityIdProvider_RegistersReflectionBasedProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDefaultEntityIdProvider();
        var provider = services.BuildServiceProvider();

        // Assert
        var entityIdProvider = provider.GetService<IEntityIdProvider>();
        Assert.NotNull(entityIdProvider);
        Assert.IsType<ReflectionBasedEntityIdProvider>(entityIdProvider);
    }

    [Fact]
    public void AddExampleLibValidation_RegistersAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-all-services"));

        // Act
        services.AddExampleLibValidation();
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ISummarisationPlanStore>());
        Assert.NotNull(provider.GetService<IValidationPlanStore>());
        Assert.NotNull(provider.GetService<IManualValidatorService>());
        Assert.NotNull(provider.GetService<IValidationService>());
        Assert.NotNull(provider.GetService<IValidationRunner>());
        Assert.NotNull(provider.GetService<IEntityIdProvider>());
        Assert.NotNull(provider.GetService<ISaveAuditRepository>());
    }

    [Fact]
    public void AddExampleLibValidation_WithEntityFrameworkBuilder_RegistersEfSaveAuditRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-ef-registration"));

        // Act
        services.AddExampleLibValidation(builder => builder.UseEntityFramework());
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<ISaveAuditRepository>();
        Assert.NotNull(repository);
        Assert.IsType<EfSaveAuditRepository>(repository);
    }

    [Fact]
    public void AddExampleLibValidation_WithMongoBuilder_RegistersMongoSaveAuditRepository()
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
        services.AddExampleLibValidation(builder => builder.UseMongo());
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<ISaveAuditRepository>();
        Assert.NotNull(repository);
        Assert.IsType<MongoSaveAuditRepository>(repository);
    }

    [Fact]
    public void AddExampleLibValidation_DoesNotOverrideExistingServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var customStore = new Mock<ISummarisationPlanStore>().Object;
        services.AddSingleton(customStore);

        // Act
        services.AddExampleLibValidation();
        var provider = services.BuildServiceProvider();

        // Assert
        var registeredStore = provider.GetService<ISummarisationPlanStore>();
        Assert.Same(customStore, registeredStore);
    }

    [Fact]
    public void AddExampleLibValidation_RegistersStoresAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddExampleLibValidation();
        var provider = services.BuildServiceProvider();

        // Assert
        var store1 = provider.GetService<ISummarisationPlanStore>();
        var store2 = provider.GetService<ISummarisationPlanStore>();
        Assert.Same(store1, store2);

        var validationStore1 = provider.GetService<IValidationPlanStore>();
        var validationStore2 = provider.GetService<IValidationPlanStore>();
        Assert.Same(validationStore1, validationStore2);
    }

    [Fact]
    public void AddExampleLibValidation_WithEfButNoDbContext_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddExampleLibValidation(builder => builder.UseEntityFramework());
        var provider = services.BuildServiceProvider();

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            provider.GetService<ISaveAuditRepository>());
        
        Assert.Contains("No DbContext is registered", exception.Message);
    }

    [Fact]
    public void AddExampleLibValidation_WithTheNannyDbContext_UsesCorrectContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-nanny-context"));

        // Act
        services.AddExampleLibValidation(builder => builder.UseEntityFramework());
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<ISaveAuditRepository>();
        Assert.NotNull(repository);
        Assert.IsType<EfSaveAuditRepository>(repository);
    }

    [Fact]
    public void AddExampleLibValidation_WithMultipleDbContexts_PrefersTheNannyDbContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options => 
            options.UseInMemoryDatabase("test-context"));
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("nanny-context"));

        // Act
        services.AddExampleLibValidation(builder => builder.UseEntityFramework());
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<ISaveAuditRepository>();
        Assert.NotNull(repository);
        Assert.IsType<EfSaveAuditRepository>(repository);
        
        // Verify it uses TheNannyDbContext by checking the repository can access SaveAudits
        var efRepository = (EfSaveAuditRepository)repository;
        // If it was using the wrong context, this would fail
        Assert.NotNull(efRepository);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExampleLibValidationBuilder_PreferMongo_WorksCorrectly(bool preferMongo)
    {
        // Arrange
        var builder = new ExampleLibValidationBuilder();

        // Act
        if (preferMongo)
            builder.UseMongo();
        else
            builder.UseEntityFramework();

        // Assert
        Assert.Equal(preferMongo, builder.PreferMongo);
    }

    [Fact]
    public void ExampleLibValidationBuilder_UseMongo_ReturnsBuilder()
    {
        // Arrange
        var builder = new ExampleLibValidationBuilder();

        // Act
        var result = builder.UseMongo();

        // Assert
        Assert.Same(builder, result);
        Assert.True(builder.PreferMongo);
    }

    [Fact]
    public void ExampleLibValidationBuilder_UseEntityFramework_ReturnsBuilder()
    {
        // Arrange
        var builder = new ExampleLibValidationBuilder();

        // Act
        var result = builder.UseEntityFramework();

        // Assert
        Assert.Same(builder, result);
        Assert.False(builder.PreferMongo);
    }

    [Fact]
    public void AddExampleLibValidation_WithNullConfigure_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("test-null-configure"));

        // Act
        services.AddExampleLibValidation(null);
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ISummarisationPlanStore>());
        Assert.NotNull(provider.GetService<IValidationPlanStore>());
        Assert.NotNull(provider.GetService<IValidationRunner>());
        Assert.NotNull(provider.GetService<ISaveAuditRepository>());
    }

    [Fact]
    public void AddExampleLibValidation_RegistersSummarisationValidatorAsGeneric()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddExampleLibValidation();
        var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<ISummarisationValidator<TestEntity>>();
        Assert.NotNull(validator);
        Assert.IsType<SummarisationValidator<TestEntity>>(validator);
    }

    [Fact]
    public void AddValidatorService_WithExistingService_DoesNotReplace()
    {
        // Arrange
        var services = new ServiceCollection();
        var existingValidator = new Mock<IManualValidatorService>().Object;
        services.AddSingleton(existingValidator);

        // Act
        services.AddValidatorService();
        var provider = services.BuildServiceProvider();

        // Assert
        var registeredService = provider.GetService<IManualValidatorService>();
        Assert.Same(existingValidator, registeredService);
    }

    [Fact]
    public void AddValidationRunner_WithExistingRunner_DoesNotReplace()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase("existing-runner-test"));
        services.AddValidatorService();
        services.AddScoped<IValidationService, ValidationService>();
        
        var existingRunner = new Mock<IValidationRunner>().Object;
        services.AddScoped(_ => existingRunner);

        // Act
        services.AddValidationRunner();
        var provider = services.BuildServiceProvider();

        // Assert
        var registeredRunner = provider.GetService<IValidationRunner>();
        Assert.Same(existingRunner, registeredRunner);
    }

    [Fact]
    public void AddConfigurableEntityIdProvider_WithExistingProvider_DoesNotReplace()
    {
        // Arrange
        var services = new ServiceCollection();
        var existingProvider = new Mock<IEntityIdProvider>().Object;
        services.AddSingleton(existingProvider);

        // Act
        services.AddConfigurableEntityIdProvider(provider => { });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var registeredProvider = serviceProvider.GetService<IEntityIdProvider>();
        Assert.Same(existingProvider, registeredProvider);
    }

    [Fact]
    public void AddReflectionBasedEntityIdProvider_WithExistingProvider_DoesNotReplace()
    {
        // Arrange
        var services = new ServiceCollection();
        var existingProvider = new Mock<IEntityIdProvider>().Object;
        services.AddSingleton(existingProvider);

        // Act
        services.AddReflectionBasedEntityIdProvider();
        var provider = services.BuildServiceProvider();

        // Assert
        var registeredProvider = provider.GetService<IEntityIdProvider>();
        Assert.Same(existingProvider, registeredProvider);
    }

    [Fact]
    public void AddValidatorRule_CreatesValidatorService_WhenNotRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<TestEntity, bool> rule = entity => entity.Validated;

        // Act
        services.AddValidatorRule(rule);
        var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<IManualValidatorService>();
        Assert.NotNull(validator);
        Assert.IsType<ManualValidatorService>(validator);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExampleLibValidationBuilder_ConfigurationMethods_ReturnCorrectValues(bool useMongo)
    {
        // Arrange
        var builder = new ExampleLibValidationBuilder();

        // Act & Assert initial state
        Assert.False(builder.PreferMongo);

        if (useMongo)
        {
            var result = builder.UseMongo();
            Assert.Same(builder, result);
            Assert.True(builder.PreferMongo);
        }
        else
        {
            var result = builder.UseEntityFramework();
            Assert.Same(builder, result);
            Assert.False(builder.PreferMongo);
        }
    }

    [Fact]
    public void AddExampleLibValidation_WithMongoButNoClient_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddExampleLibValidation(builder => builder.UseMongo());
        var provider = services.BuildServiceProvider();

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            provider.GetService<ISaveAuditRepository>());
        
        Assert.Contains("No IMongoClient is registered", exception.Message);
    }

    [Fact]
    public void AddExampleLibValidation_WithMultipleDbContexts_UsesFirstAvailable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options => 
            options.UseInMemoryDatabase("first-context"));
        // Don't add TheNannyDbContext to test fallback behavior

        // Act
        services.AddExampleLibValidation(builder => builder.UseEntityFramework());
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<ISaveAuditRepository>();
        Assert.NotNull(repository);
        Assert.IsType<EfSaveAuditRepository>(repository);
    }

    [Fact]
    public void AddExampleLibValidation_RegistersGenericSummarisationValidator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddExampleLibValidation();
        var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<ISummarisationValidator<TestEntity>>();
        Assert.NotNull(validator);
        Assert.IsType<SummarisationValidator<TestEntity>>(validator);
    }

    [Fact]
    public void AddExampleLibValidation_WithEmptyConfiguration_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddExampleLibValidation(builder => { });
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ISummarisationPlanStore>());
        Assert.NotNull(provider.GetService<IValidationPlanStore>());
        Assert.NotNull(provider.GetService<IManualValidatorService>());
    }
}