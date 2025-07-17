using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;

namespace ExampleLib.Tests.TestHelpers;

/// <summary>
/// Test helper builder for configuring ExampleLib services in unit tests.
/// This class provides test-specific convenience methods and overrides for testing scenarios.
/// </summary>
public class ExampleLibTestBuilder
{
    private readonly IServiceCollection _services;
    private bool _useInMemoryDatabase = true;
    private string _databaseName = Guid.NewGuid().ToString();
    private string _applicationName = "TestApp";
    private bool _useEntityFramework = true;
    private readonly List<(Type EntityType, object Selector, ThresholdType ThresholdType, decimal ThresholdValue)> _summarisationPlans = new();
    private readonly List<(Type EntityType, double Threshold, ValidationStrategy Strategy)> _validationPlans = new();

    public ExampleLibTestBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Configure the test builder to use a specific in-memory database name.
    /// </summary>
    /// <param name="databaseName">The name for the in-memory database</param>
    /// <returns>The builder for chaining</returns>
    public ExampleLibTestBuilder WithInMemoryDatabase(string databaseName)
    {
        _useInMemoryDatabase = true;
        _databaseName = databaseName;
        return this;
    }

    /// <summary>
    /// Configure the test builder to use a real database (not recommended for unit tests).
    /// </summary>
    /// <returns>The builder for chaining</returns>
    public ExampleLibTestBuilder WithRealDatabase()
    {
        _useInMemoryDatabase = false;
        return this;
    }

    /// <summary>
    /// Configure the application name for testing.
    /// </summary>
    /// <param name="applicationName">The application name</param>
    /// <returns>The builder for chaining</returns>
    public ExampleLibTestBuilder WithApplicationName(string applicationName)
    {
        _applicationName = applicationName;
        return this;
    }

    /// <summary>
    /// Configure to use Entity Framework (default).
    /// </summary>
    /// <returns>The builder for chaining</returns>
    public ExampleLibTestBuilder UseEntityFramework()
    {
        _useEntityFramework = true;
        return this;
    }

    /// <summary>
    /// Configure to use MongoDB.
    /// </summary>
    /// <returns>The builder for chaining</returns>
    public ExampleLibTestBuilder UseMongo()
    {
        _useEntityFramework = false;
        return this;
    }

    /// <summary>
    /// Configures ExampleLib with common test defaults.
    /// </summary>
    /// <param name="applicationName">The application name for tests (default: "TestApp")</param>
    /// <returns>The builder for chaining</returns>
    public ExampleLibTestBuilder WithTestDefaults(string applicationName = "TestApp")
    {
        _applicationName = applicationName;
        return this;
    }

    /// <summary>
    /// Configure with test entities and validation rules commonly used in tests.
    /// </summary>
    /// <returns>The builder for chaining</returns>
    public ExampleLibTestBuilder WithTestEntities()
    {
        return WithTestEntities<TestEntity>();
    }

    /// <summary>
    /// Configure with specified test entity type and basic validation rules.
    /// </summary>
    /// <typeparam name="T">The test entity type</typeparam>
    /// <returns>The builder for chaining</returns>
    public ExampleLibTestBuilder WithTestEntities<T>()
        where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        // Add default summarisation plan for the entity
        AddSummarisationPlan<T>(entity => 0m, ThresholdType.RawDifference, 5.0m);
        AddValidationPlan<T>(threshold: 10.0, ValidationStrategy.Count);
        AddValidationRules<T>(entity => entity.Validated);
        return this;
    }

    /// <summary>
    /// Add a summarisation plan for an entity type.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="selector">Value selector function</param>
    /// <param name="thresholdType">Threshold type</param>
    /// <param name="thresholdValue">Threshold value</param>
    /// <returns>The builder for chaining</returns>
    public ExampleLibTestBuilder AddSummarisationPlan<T>(
        Func<T, decimal> selector, 
        ThresholdType thresholdType, 
        decimal thresholdValue)
        where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        _summarisationPlans.Add((typeof(T), selector, thresholdType, thresholdValue));
        return this;
    }

    /// <summary>
    /// Add a validation plan for an entity type.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="threshold">Validation threshold</param>
    /// <param name="strategy">Validation strategy</param>
    /// <returns>The builder for chaining</returns>
    public ExampleLibTestBuilder AddValidationPlan<T>(double threshold, ValidationStrategy strategy = ValidationStrategy.Count)
        where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        _validationPlans.Add((typeof(T), threshold, strategy));
        return this;
    }

    /// <summary>
    /// Build and configure all services for testing.
    /// </summary>
    /// <returns>The configured service provider</returns>
    public IServiceProvider Build()
    {
        // ALWAYS configure database context first for tests
        if (_useEntityFramework && _useInMemoryDatabase)
        {
            // Only add DbContext if not already registered
            if (!_services.Any(s => s.ServiceType == typeof(TheNannyDbContext)))
            {
                _services.AddDbContext<TheNannyDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));
            }
        }

        // For MongoDB tests, add a properly configured mock mongo client
        if (!_useEntityFramework)
        {
            var mockClient = new Mock<IMongoClient>();
            var mockDatabase = new Mock<IMongoDatabase>();
            var mockCollection = new Mock<IMongoCollection<SaveAudit>>();
            
            // Setup the mock chain
            mockClient.Setup(c => c.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
                .Returns(mockDatabase.Object);
            mockDatabase.Setup(d => d.GetCollection<SaveAudit>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
                .Returns(mockCollection.Object);
            
            _services.AddSingleton(mockClient.Object);
        }

        // Register ExampleLib validation services
        _services.AddExampleLibValidation(builder =>
        {
            if (_useEntityFramework)
                builder.UseEntityFramework();
            else
                builder.UseMongo();
        });

        // Override the application name provider to ensure tests have a known value
        _services.RemoveAll<IApplicationNameProvider>();
        _services.AddSingleton<IApplicationNameProvider>(new StaticApplicationNameProvider(_applicationName));

        // Build the provider
        var provider = _services.BuildServiceProvider();

        // Configure plans after building the provider
        ConfigurePlans(provider);

        return provider;
    }

    private void ConfigurePlans(IServiceProvider provider)
    {
        // Configure summarisation plans
        var summarisationStore = provider.GetService<ISummarisationPlanStore>();
        if (summarisationStore is InMemorySummarisationPlanStore memoryStore)
        {
            foreach (var (entityType, selector, thresholdType, thresholdValue) in _summarisationPlans)
            {
                // Create the generic SummarisationPlan<T> using reflection
                var planType = typeof(SummarisationPlan<>).MakeGenericType(entityType);
                var plan = Activator.CreateInstance(planType, selector, thresholdType, thresholdValue);
                
                // Use reflection to call the generic AddPlan method
                var addPlanMethod = typeof(InMemorySummarisationPlanStore).GetMethod("AddPlan");
                if (addPlanMethod != null && plan != null)
                {
                    var genericMethod = addPlanMethod.MakeGenericMethod(entityType);
                    genericMethod.Invoke(memoryStore, new object[] { plan });
                }
            }
        }

        // Configure validation plans
        var validationStore = provider.GetService<IValidationPlanStore>();
        if (validationStore is InMemoryValidationPlanStore validationMemoryStore)
        {
            foreach (var (entityType, threshold, strategy) in _validationPlans)
            {
                var plan = new ValidationPlan(entityType, threshold, strategy);
                validationMemoryStore.AddPlan(plan);
            }
        }
    }

    /// <summary>
    /// Add test validation rules for a specific entity type.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="rules">Validation rules to add</param>
    /// <returns>The builder for chaining</returns>
    public ExampleLibTestBuilder AddValidationRules<T>(params Func<T, bool>[] rules)
        where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        foreach (var rule in rules)
        {
            _services.AddValidatorRule(rule);
        }
        return this;
    }

    /// <summary>
    /// Create a service collection and test builder for quick test setup.
    /// </summary>
    /// <returns>A new test builder with a fresh service collection</returns>
    public static ExampleLibTestBuilder Create()
    {
        return new ExampleLibTestBuilder(new ServiceCollection());
    }

    /// <summary>
    /// Create a test builder with an existing service collection.
    /// </summary>
    /// <param name="services">The service collection to use</param>
    /// <returns>A new test builder</returns>
    public static ExampleLibTestBuilder Create(IServiceCollection services)
    {
        return new ExampleLibTestBuilder(services);
    }
}

/// <summary>
/// Common test entity for use in ExampleLib tests.
/// </summary>
public class TestEntity : IValidatable, IBaseEntity, IRootEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public bool Validated { get; set; }
}

/// <summary>
/// Extension methods for IServiceCollection to simplify test setup.
/// </summary>
public static class ServiceCollectionTestExtensions
{
    /// <summary>
    /// Configure ExampleLib for testing with sensible defaults.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddExampleLibForTesting(
        this IServiceCollection services,
        Action<ExampleLibTestBuilder>? configure = null)
    {
        var builder = new ExampleLibTestBuilder(services);
        
        // Apply test defaults first
        builder.WithTestDefaults();
        
        // Then apply any custom configuration
        configure?.Invoke(builder);
        
        // Build the configuration
        builder.Build();
        return services;
    }

    /// <summary>
    /// Add a test-specific in-memory database context.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="databaseName">Optional database name (default: random GUID)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTestDatabase(
        this IServiceCollection services,
        string? databaseName = null)
    {
        databaseName ??= Guid.NewGuid().ToString();
        services.AddDbContext<TheNannyDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        return services;
    }
}