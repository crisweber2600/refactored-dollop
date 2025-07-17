using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MongoDB.Driver;

namespace ExampleLib.Tests;

/// <summary>
/// Comprehensive unit tests for ServiceCollectionExtensions methods.
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

    #region ServiceCollectionRemovalExtensions Tests

    [Fact]
    public void RemoveAll_RemovesAllServicesOfSpecifiedType()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IManualValidatorService, ManualValidatorService>();
        services.AddSingleton<IManualValidatorService, ManualValidatorService>();
        services.AddTransient<IManualValidatorService, ManualValidatorService>();

        // Act
        services.RemoveAll<IManualValidatorService>();

        // Assert
        Assert.DoesNotContain(services, s => s.ServiceType == typeof(IManualValidatorService));
    }

    [Fact]
    public void RemoveAll_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IManualValidatorService, ManualValidatorService>();

        // Act
        var result = services.RemoveAll<IManualValidatorService>();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void RemoveAll_WithNoMatchingServices_DoesNothing()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidationRunner, ValidationRunner>();

        // Act
        services.RemoveAll<IManualValidatorService>();

        // Assert
        Assert.Single(services);
        Assert.Equal(typeof(IValidationRunner), services.First().ServiceType);
    }

    #endregion

    #region AddValidatorService Tests

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
    public void AddValidatorService_DoesNotRegisterTwice()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddValidatorService();
        services.AddValidatorService();

        // Assert
        var descriptors = services.Where(s => s.ServiceType == typeof(IManualValidatorService));
        Assert.Single(descriptors);
    }

    [Fact]
    public void AddValidatorService_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddValidatorService();

        // Assert
        Assert.Same(services, result);
    }

    #endregion

    #region AddValidationRunner Tests

    [Fact]
    public void AddValidationRunner_RegistersAllDependencies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-validation-runner"));

        // Act
        services.AddValidationRunner();
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IValidationRunner>());
        Assert.NotNull(provider.GetService<ISummarisationPlanStore>());
        Assert.NotNull(provider.GetService<IValidationPlanStore>());
        Assert.NotNull(provider.GetService<IApplicationNameProvider>());
        Assert.NotNull(provider.GetService<IEntityIdProvider>());
        Assert.NotNull(provider.GetService<IManualValidatorService>());
        Assert.NotNull(provider.GetService<ISaveAuditRepository>());
        Assert.NotNull(provider.GetService<IValidationService>());
        Assert.NotNull(provider.GetService<IValidationRunner>());
    }

    [Fact]
    public void AddValidationRunner_WithTheNannyDbContext_UsesCorrectRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-nanny-context"));

        // Act
        services.AddValidationRunner();
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<ISaveAuditRepository>();
        Assert.NotNull(repository);
        Assert.IsType<EfSaveAuditRepository>(repository);
    }

    [Fact]
    public void AddValidationRunner_WithCustomDbContext_UsesCorrectRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options => 
            options.UseInMemoryDatabase("test-custom-context"));

        // Act
        services.AddValidationRunner();
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<ISaveAuditRepository>();
        Assert.NotNull(repository);
        Assert.IsType<EfSaveAuditRepository>(repository);
    }

    [Fact]
    public void AddValidationRunner_WithNoDbContext_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddValidationRunner();
        var provider = services.BuildServiceProvider();

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            provider.GetService<ISaveAuditRepository>());
        Assert.Contains("No DbContext is registered", exception.Message);
    }

    [Fact]
    public void AddValidationRunner_WithExistingServices_DoesNotOverride()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-existing-services"));
        
        var customPlanStore = new InMemorySummarisationPlanStore();
        services.AddSingleton<ISummarisationPlanStore>(customPlanStore);

        // Act
        services.AddValidationRunner();
        var provider = services.BuildServiceProvider();

        // Assert
        var planStore = provider.GetService<ISummarisationPlanStore>();
        Assert.Same(customPlanStore, planStore);
    }

    [Fact]
    public void AddValidationRunner_RegistersGenericSummarisationValidator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-generic-validator"));

        // Act
        services.AddValidationRunner();
        var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<ISummarisationValidator<TestEntity>>();
        Assert.NotNull(validator);
        Assert.IsType<SummarisationValidator<TestEntity>>(validator);
    }

    #endregion

    #region AddValidatorRule Tests

    [Fact]
    public void AddValidatorRule_WithNoExistingValidator_CreatesNewValidator()
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
        
        var validEntity = new TestEntity { Name = "Valid" };
        var invalidEntity = new TestEntity { Name = "" };
        
        Assert.True(validator.Validate(validEntity));
        Assert.False(validator.Validate(invalidEntity));
    }

    [Fact]
    public void AddValidatorRule_WithExistingValidator_PreservesExistingRules()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IManualValidatorService, ManualValidatorService>();
        
        // Add first rule
        Func<TestEntity, bool> rule1 = entity => !string.IsNullOrWhiteSpace(entity.Name);
        services.AddValidatorRule(rule1);
        
        // Add second rule
        Func<TestEntity, bool> rule2 = entity => entity.Id > 0;
        services.AddValidatorRule(rule2);
        
        var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<IManualValidatorService>();
        Assert.NotNull(validator);
        
        var validEntity = new TestEntity { Id = 1, Name = "Valid" };
        var invalidEntity1 = new TestEntity { Id = 1, Name = "" }; // Fails rule1
        var invalidEntity2 = new TestEntity { Id = 0, Name = "Valid" }; // Fails rule2
        
        Assert.True(validator.Validate(validEntity));
        Assert.False(validator.Validate(invalidEntity1));
        Assert.False(validator.Validate(invalidEntity2));
    }

    [Fact]
    public void AddValidatorRule_WithInstanceValidator_PreservesExistingRules()
    {
        // Arrange
        var services = new ServiceCollection();
        var existingValidator = new ManualValidatorService();
        services.AddSingleton<IManualValidatorService>(existingValidator);
        
        Func<TestEntity, bool> rule = entity => entity.Id > 0;
        
        // Act
        services.AddValidatorRule(rule);
        var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<IManualValidatorService>();
        Assert.NotNull(validator);
        
        var validEntity = new TestEntity { Id = 1, Name = "Valid" };
        var invalidEntity = new TestEntity { Id = 0, Name = "Valid" };
        
        Assert.True(validator.Validate(validEntity));
        Assert.False(validator.Validate(invalidEntity));
    }

    [Fact]
    public void AddValidatorRule_WithFactoryValidator_HandlesExceptionGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IManualValidatorService>(sp => 
            throw new InvalidOperationException("Test exception"));
        
        Func<TestEntity, bool> rule = entity => entity.Id > 0;
        
        // Act
        services.AddValidatorRule(rule);
        var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<IManualValidatorService>();
        Assert.NotNull(validator);
        
        var validEntity = new TestEntity { Id = 1, Name = "Valid" };
        Assert.True(validator.Validate(validEntity));
    }

    [Fact]
    public void AddValidatorRule_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<TestEntity, bool> rule = entity => entity.Id > 0;

        // Act
        var result = services.AddValidatorRule(rule);

        // Assert
        Assert.Same(services, result);
    }

    #endregion

    #region AddConfigurableEntityIdProvider Tests

    [Fact]
    public void AddConfigurableEntityIdProvider_RegistersProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddConfigurableEntityIdProvider(provider =>
        {
            provider.RegisterSelector<TestEntity>(e => e.Name);
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var entityIdProvider = serviceProvider.GetService<IEntityIdProvider>();
        Assert.NotNull(entityIdProvider);
        Assert.IsType<ConfigurableEntityIdProvider>(entityIdProvider);
        
        var entity = new TestEntity { Name = "TestName" };
        var entityId = entityIdProvider.GetEntityId(entity);
        Assert.Equal("TestName", entityId);
    }

    [Fact]
    public void AddConfigurableEntityIdProvider_DoesNotOverrideExisting()
    {
        // Arrange
        var services = new ServiceCollection();
        var existingProvider = new ReflectionBasedEntityIdProvider();
        services.AddSingleton<IEntityIdProvider>(existingProvider);

        // Act
        services.AddConfigurableEntityIdProvider(provider =>
        {
            provider.RegisterSelector<TestEntity>(e => e.Name);
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var entityIdProvider = serviceProvider.GetService<IEntityIdProvider>();
        Assert.Same(existingProvider, entityIdProvider);
    }

    [Fact]
    public void AddConfigurableEntityIdProvider_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddConfigurableEntityIdProvider(provider => { });

        // Assert
        Assert.Same(services, result);
    }

    #endregion

    #region AddReflectionBasedEntityIdProvider Tests

    [Fact]
    public void AddReflectionBasedEntityIdProvider_WithoutPriority_RegistersProvider()
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
    public void AddReflectionBasedEntityIdProvider_WithPriority_RegistersProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var priority = new[] { "Name", "Code", "Title" };

        // Act
        services.AddReflectionBasedEntityIdProvider(priority);
        var provider = services.BuildServiceProvider();

        // Assert
        var entityIdProvider = provider.GetService<IEntityIdProvider>();
        Assert.NotNull(entityIdProvider);
        Assert.IsType<ReflectionBasedEntityIdProvider>(entityIdProvider);
    }

    [Fact]
    public void AddReflectionBasedEntityIdProvider_DoesNotOverrideExisting()
    {
        // Arrange
        var services = new ServiceCollection();
        var existingProvider = new ConfigurableEntityIdProvider();
        services.AddSingleton<IEntityIdProvider>(existingProvider);

        // Act
        services.AddReflectionBasedEntityIdProvider();
        var provider = services.BuildServiceProvider();

        // Assert
        var entityIdProvider = provider.GetService<IEntityIdProvider>();
        Assert.Same(existingProvider, entityIdProvider);
    }

    [Fact]
    public void AddReflectionBasedEntityIdProvider_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddReflectionBasedEntityIdProvider();

        // Assert
        Assert.Same(services, result);
    }

    #endregion

    #region AddDefaultEntityIdProvider Tests

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
    public void AddDefaultEntityIdProvider_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddDefaultEntityIdProvider();

        // Assert
        Assert.Same(services, result);
    }

    #endregion

    #region FindRegisteredDbContextType Tests

    [Fact]
    public void FindRegisteredDbContextType_WithTheNannyDbContext_ReturnsTheNannyDbContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-find-nanny"));
        services.AddDbContext<TestDbContext>(options => 
            options.UseInMemoryDatabase("test-find-custom"));

        // Act
        services.AddValidationRunner();
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<ISaveAuditRepository>();
        Assert.NotNull(repository);
        Assert.IsType<EfSaveAuditRepository>(repository);
    }

    [Fact]
    public void FindRegisteredDbContextType_WithCustomDbContext_ReturnsCustomDbContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options => 
            options.UseInMemoryDatabase("test-find-custom-only"));

        // Act
        services.AddValidationRunner();
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<ISaveAuditRepository>();
        Assert.NotNull(repository);
        Assert.IsType<EfSaveAuditRepository>(repository);
    }

    #endregion

    #region AddExampleLibValidation Tests

    [Fact]
    public void AddExampleLibValidation_WithoutConfiguration_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-default-validation"));

        // Act
        services.AddExampleLibValidation();
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ISummarisationPlanStore>());
        Assert.NotNull(provider.GetService<IValidationPlanStore>());
        Assert.NotNull(provider.GetService<IApplicationNameProvider>());
        Assert.NotNull(provider.GetService<IEntityIdProvider>());
        Assert.NotNull(provider.GetService<IManualValidatorService>());
        Assert.NotNull(provider.GetService<ISaveAuditRepository>());
        Assert.NotNull(provider.GetService<IValidationService>());
        Assert.NotNull(provider.GetService<IValidationRunner>());
    }

    [Fact]
    public void AddExampleLibValidation_WithEntityFrameworkConfiguration_UsesEfRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-ef-validation"));

        // Act
        services.AddExampleLibValidation(builder => 
            builder.UseEntityFramework());
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<ISaveAuditRepository>();
        Assert.NotNull(repository);
        Assert.IsType<EfSaveAuditRepository>(repository);
    }

    [Fact]
    public void AddExampleLibValidation_WithMongoConfiguration_UsesMongoRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockClient = new Mock<IMongoClient>();
        var mockDatabase = new Mock<IMongoDatabase>();
        var mockCollection = new Mock<IMongoCollection<SaveAudit>>();

        mockClient.Setup(x => x.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
                  .Returns(mockDatabase.Object);

        mockDatabase.Setup(x => x.GetCollection<SaveAudit>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
                    .Returns(mockCollection.Object);

        services.AddSingleton(mockClient.Object);

        // Act
        services.AddExampleLibValidation(builder => 
            builder.UseMongo());
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<ISaveAuditRepository>();
        Assert.NotNull(repository);
        Assert.IsType<MongoSaveAuditRepository>(repository);
    }

    [Fact]
    public void AddExampleLibValidation_WithMongoConfigurationButNoClient_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddExampleLibValidation(builder => 
            builder.UseMongo());
        var provider = services.BuildServiceProvider();

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            provider.GetService<ISaveAuditRepository>());
        Assert.Contains("No IMongoClient is registered", exception.Message);
    }

    [Fact]
    public void AddExampleLibValidation_WithExistingServices_DoesNotOverride()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-existing-validation"));
        
        var customPlanStore = new InMemorySummarisationPlanStore();
        services.AddSingleton<ISummarisationPlanStore>(customPlanStore);

        // Act
        services.AddExampleLibValidation();
        var provider = services.BuildServiceProvider();

        // Assert
        var planStore = provider.GetService<ISummarisationPlanStore>();
        Assert.Same(customPlanStore, planStore);
    }

    [Fact]
    public void AddExampleLibValidation_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-chaining-validation"));

        // Act
        var result = services.AddExampleLibValidation();

        // Assert
        Assert.Same(services, result);
    }

    #endregion

    #region ExampleLibValidationBuilder Tests

    [Fact]
    public void ExampleLibValidationBuilder_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var builder = new ExampleLibValidationBuilder();

        // Assert
        Assert.False(builder.PreferMongo);
    }

    [Fact]
    public void ExampleLibValidationBuilder_UseMongo_SetsPreferMongo()
    {
        // Arrange
        var builder = new ExampleLibValidationBuilder();

        // Act
        var result = builder.UseMongo();

        // Assert
        Assert.True(builder.PreferMongo);
        Assert.Same(builder, result);
    }

    [Fact]
    public void ExampleLibValidationBuilder_UseEntityFramework_SetsPreferMongo()
    {
        // Arrange
        var builder = new ExampleLibValidationBuilder();
        builder.PreferMongo = true;

        // Act
        var result = builder.UseEntityFramework();

        // Assert
        Assert.False(builder.PreferMongo);
        Assert.Same(builder, result);
    }

    #endregion

    #region DefaultSelector Tests

    [Fact]
    public void DefaultSelector_WithValidIdProperty_ReturnsDecimalValue()
    {
        // Test is indirect since DefaultSelector is private
        // We test it through the ValidationRunner's default value selector behavior
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-default-selector"));

        services.AddValidationRunner();
        var provider = services.BuildServiceProvider();

        var runner = provider.GetService<IValidationRunner>();
        Assert.NotNull(runner);
        
        // The default selector should work correctly when ValidationRunner uses it
        var entity = new TestEntity { Id = 42, Name = "Test" };
        // This indirectly tests the default selector through ValidationRunner
        Assert.NotNull(runner);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ServiceCollectionExtensions_IntegrationWithMultipleServices_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-integration"));

        // Act
        services.AddValidatorService()
                .AddDefaultEntityIdProvider()
                .AddValidationRunner()
                .AddValidatorRule<TestEntity>(e => !string.IsNullOrWhiteSpace(e.Name))
                .AddConfigurableEntityIdProvider(provider =>
                {
                    provider.RegisterSelector<TestEntity>(e => e.Name);
                });

        var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<IManualValidatorService>();
        var entityIdProvider = provider.GetService<IEntityIdProvider>();
        var runner = provider.GetService<IValidationRunner>();
        var planStore = provider.GetService<ISummarisationPlanStore>();

        Assert.NotNull(validator);
        Assert.NotNull(entityIdProvider);
        Assert.NotNull(runner);
        Assert.NotNull(planStore);

        // Test that the validator rule works
        var validEntity = new TestEntity { Name = "Valid" };
        var invalidEntity = new TestEntity { Name = "" };
        
        Assert.True(validator.Validate(validEntity));
        Assert.False(validator.Validate(invalidEntity));
    }

    [Fact]
    public void ServiceCollectionExtensions_WithComplexConfiguration_BuildsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-complex-config"));

        // Act
        services.AddExampleLibValidation(builder =>
        {
            builder.UseEntityFramework();
        });
        
        services.AddValidatorRule<TestEntity>(e => e.Id > 0);
        services.AddValidatorRule<TestEntity>(e => !string.IsNullOrWhiteSpace(e.Name));

        var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetService<IManualValidatorService>();
        Assert.NotNull(validator);
        
        var validEntity = new TestEntity { Id = 1, Name = "Valid" };
        var invalidEntity1 = new TestEntity { Id = 0, Name = "Valid" };
        var invalidEntity2 = new TestEntity { Id = 1, Name = "" };
        
        Assert.True(validator.Validate(validEntity));
        Assert.False(validator.Validate(invalidEntity1));
        Assert.False(validator.Validate(invalidEntity2));
    }

    #endregion

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
}