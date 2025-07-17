using System.Reflection;
using ExampleLib.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Extension methods for IServiceCollection to remove services.
/// </summary>
public static class ServiceCollectionRemovalExtensions
{
    /// <summary>
    /// Removes all service descriptors of the specified type from the collection.
    /// </summary>
    /// <typeparam name="T">The type of service to remove</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection RemoveAll<T>(this IServiceCollection services)
    {
        var descriptorsToRemove = services.Where(descriptor => descriptor.ServiceType == typeof(T)).ToList();
        foreach (var descriptor in descriptorsToRemove)
        {
            services.Remove(descriptor);
        }
        return services;
    }
}

/// <summary>
/// Service registration helpers for the summarisation validation workflow.
/// </summary>
public static class ServiceCollectionExtensions
{
    internal static decimal DefaultSelector<T>(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // Look for decimal properties first
        var decimalProperties = typeof(T).GetProperties()
            .Where(p => p.PropertyType == typeof(decimal) && p.CanRead)
            .ToList();

        if (decimalProperties.Count > 0)
        {
            // Return the first decimal property value
            var value = decimalProperties[0].GetValue(entity);
            return value != null ? (decimal)value : 0m;
        }

        // If no decimal properties found, try to use the Id property
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty != null && idProperty.CanRead)
        {
            var idValue = idProperty.GetValue(entity);
            if (idValue != null)
            {
                // Convert to decimal - handle int, long, and other numeric types
                if (idValue is int intId)
                    return (decimal)intId;
                if (idValue is long longId)
                    return (decimal)longId;
                if (idValue is decimal decimalId)
                    return decimalId;
                if (idValue is double doubleId)
                    return (decimal)doubleId;
                if (idValue is float floatId)
                    return (decimal)floatId;
                
                // Try to convert using Convert.ToDecimal as fallback
                try
                {
                    return Convert.ToDecimal(idValue);
                }
                catch
                {
                    // If conversion fails, return 0
                    return 0m;
                }
            }
        }

        return 0m;
    }

    /// <summary>
    /// Simplified validation setup that configures 90% of the validation infrastructure with one call.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="applicationName">The application name (will be retrieved from IApplicationNameProvider if not specified)</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddExampleLibValidation(
        this IServiceCollection services, 
        string? applicationName = null)
    {
        // Core validation infrastructure (90% of setup) - only register if not already present
        if (!services.Any(x => x.ServiceType == typeof(ISummarisationPlanStore)))
        {
            services.AddSingleton<ISummarisationPlanStore, InMemorySummarisationPlanStore>();
        }
        
        if (!services.Any(x => x.ServiceType == typeof(IValidationPlanStore)))
        {
            services.AddSingleton<IValidationPlanStore, InMemoryValidationPlanStore>();
        }
        
        // Use applicationName if provided, otherwise use IApplicationNameProvider or default
        if (!string.IsNullOrEmpty(applicationName))
        {
            // Only register if no IApplicationNameProvider is already registered
            if (!services.Any(x => x.ServiceType == typeof(IApplicationNameProvider)))
            {
                services.AddSingleton<IApplicationNameProvider>(new StaticApplicationNameProvider(applicationName));
            }
        }
        else if (!services.Any(x => x.ServiceType == typeof(IApplicationNameProvider)))
        {
            // If no applicationName provided and no IApplicationNameProvider registered, use default
            services.AddSingleton<IApplicationNameProvider>(new StaticApplicationNameProvider("ExampleLib-DefaultApp"));
        }
        
        services.AddDefaultEntityIdProvider();
        services.AddValidatorService();
        
        // Register save audit repository with smart detection - only if not already registered
        if (!services.Any(x => x.ServiceType == typeof(ISaveAuditRepository)))
        {
            services.AddScoped<ISaveAuditRepository>(sp =>
            {
                // Try MongoDB first if available
                var mongoClient = sp.GetService<IMongoClient>();
                if (mongoClient != null)
                {
                    return new MongoSaveAuditRepository(mongoClient);
                }
                
                // Fall back to EF Core
                var dbContext = sp.GetService<TheNannyDbContext>() ?? 
                               sp.GetService<DbContext>();
                if (dbContext != null)
                {
                    return new EfSaveAuditRepository(dbContext);
                }
                
                throw new InvalidOperationException(
                    "No database provider found. Register either a DbContext or IMongoClient.");
            });
        }
        
        // Core validation services - only register if not already present
        if (!services.Any(x => x.ServiceType == typeof(IValidationService)))
        {
            services.AddScoped<IValidationService>(sp => new ValidationService(
                sp.GetRequiredService<ISummarisationPlanStore>(),
                sp.GetRequiredService<ISaveAuditRepository>(),
                sp,
                sp.GetRequiredService<IApplicationNameProvider>(),
                sp.GetService<IEntityIdProvider>()));
        }
        
        if (!services.Any(x => x.ServiceType == typeof(ISummarisationValidator<>)))
        {
            services.AddSingleton(typeof(ISummarisationValidator<>), typeof(SummarisationValidator<>));
        }
        
        services.AddValidationRunner();
        
        return services;
    }

    /// <summary>
    /// Simplified validation setup that configures 90% of the validation infrastructure with one call.
    /// This overload accepts a configuration action for more advanced configuration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration action for advanced setup</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddExampleLibValidation(
        this IServiceCollection services, 
        Action<ExampleLibValidationBuilder> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        // Use the legacy method which handles the configuration properly
        return services.AddExampleLibValidationLegacy(configure);
    }

    /// <summary>
    /// Configure validation plans and rules for specific entities.
    /// Call this after AddExampleLibValidation() to set up entity-specific validations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection ConfigureValidation<TEntity>(
        this IServiceCollection services,
        Action<ValidationConfiguration<TEntity>> configure)
        where TEntity : class, IValidatable, IBaseEntity, IRootEntity
    {
        var config = new ValidationConfiguration<TEntity>(services);
        configure(config);
        return services;
    }

    /// <summary>
    /// Register <see cref="ManualValidatorService"/> and the rule dictionary as singletons.
    /// </summary>
    public static IServiceCollection AddValidatorService(this IServiceCollection services)
    {
        if (!services.Any(x => x.ServiceType == typeof(IManualValidatorService)))
        {
            services.AddSingleton<IManualValidatorService, ManualValidatorService>();
        }
        return services;
    }

    /// <summary>
    /// Register <see cref="ValidationRunner"/> for executing all validations including sequence validation.
    /// This method ensures all required dependencies are registered before registering ValidationRunner.
    /// </summary>
    public static IServiceCollection AddValidationRunner(this IServiceCollection services)
    {
        // Ensure dependencies are registered first
        if (!services.Any(x => x.ServiceType == typeof(ISummarisationPlanStore)))
        {
            services.AddSingleton<ISummarisationPlanStore, InMemorySummarisationPlanStore>();
        }

        if (!services.Any(x => x.ServiceType == typeof(IValidationPlanStore)))
        {
            services.AddSingleton<IValidationPlanStore, InMemoryValidationPlanStore>();
        }

        if (!services.Any(x => x.ServiceType == typeof(IApplicationNameProvider)))
        {
            services.AddSingleton<IApplicationNameProvider>(sp => 
                new StaticApplicationNameProvider("ExampleLib-DefaultApp"));
        }

        if (!services.Any(x => x.ServiceType == typeof(IEntityIdProvider)))
        {
            services.AddDefaultEntityIdProvider();
        }

        if (!services.Any(x => x.ServiceType == typeof(IManualValidatorService)))
        {
            services.AddValidatorService();
        }

        // Register ISaveAuditRepository if not already registered - this is required for IValidationService
        if (!services.Any(x => x.ServiceType == typeof(ISaveAuditRepository)))
        {
            // Find what DbContext types are registered before building the provider
            var dbContextType = FindRegisteredDbContextType(services);
            
            // Use a deferred factory that resolves DbContext at runtime
            services.AddScoped<ISaveAuditRepository>(sp =>
            {
                // Try to get TheNannyDbContext first
                var theNannyDbContext = sp.GetService<TheNannyDbContext>();
                if (theNannyDbContext != null)
                {
                    return new EfSaveAuditRepository(theNannyDbContext);
                }
                
                // Use the found DbContext type
                if (dbContextType != null)
                {
                    var dbContext = (DbContext)sp.GetRequiredService(dbContextType);
                    return new EfSaveAuditRepository(dbContext);
                }
                
                throw new InvalidOperationException(
                    "No DbContext is registered. When using Entity Framework with ExampleLib, " +
                    "you must register a DbContext (preferably TheNannyDbContext) using " +
                    "services.AddDbContext<TheNannyDbContext>() or similar.");
            });
        }

        // Register IValidationService if not already registered
        if (!services.Any(x => x.ServiceType == typeof(IValidationService)))
        {
            services.AddScoped<IValidationService>(sp => new ValidationService(
                sp.GetRequiredService<ISummarisationPlanStore>(),
                sp.GetRequiredService<ISaveAuditRepository>(),
                sp,
                sp.GetRequiredService<IApplicationNameProvider>(),
                sp.GetService<IEntityIdProvider>()));
        }

        // Register ISummarisationValidator<> if not already registered
        if (!services.Any(x => x.ServiceType == typeof(ISummarisationValidator<>)))
        {
            services.AddSingleton(typeof(ISummarisationValidator<>), typeof(SummarisationValidator<>));
        }

        // Finally, register IValidationRunner
        if (!services.Any(x => x.ServiceType == typeof(IValidationRunner)))
        {
            services.AddScoped<IValidationRunner>(sp => new ValidationRunner(
                sp.GetRequiredService<IValidationService>(),
                sp.GetRequiredService<IManualValidatorService>(),
                sp));
        }

        return services;
    }

    /// <summary>
    /// Add a manual validation rule for the specified type.
    /// This method modifies the existing ManualValidatorService registration to include the new rule.
    /// </summary>
    public static IServiceCollection AddValidatorRule<T>(this IServiceCollection services, Func<T, bool> rule)
        where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        if (rule == null)
            throw new ArgumentNullException(nameof(rule));

        // Find existing ManualValidatorService registration
        var existingDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IManualValidatorService));
        
        if (existingDescriptor != null)
        {
            // Get the existing validator instance to preserve its rules
            ManualValidatorService? existingValidator = null;
            
            if (existingDescriptor.ImplementationInstance is ManualValidatorService instanceValidator)
            {
                existingValidator = instanceValidator;
            }
            else if (existingDescriptor.ImplementationFactory != null)
            {
                // Try to resolve the existing factory to get current rules
                try
                {
                    var tempProvider = services.BuildServiceProvider();
                    existingValidator = tempProvider.GetService<IManualValidatorService>() as ManualValidatorService;
                    tempProvider.Dispose();
                }
                catch
                {
                    // If we can't resolve, start with an empty validator
                    existingValidator = null;
                }
            }

            // Remove the existing registration
            services.Remove(existingDescriptor);
            
            // Create a new registration that includes the existing rules plus the new one
            services.AddSingleton<IManualValidatorService>(sp =>
            {
                var validator = existingValidator != null 
                    ? new ManualValidatorService(new Dictionary<Type, List<Func<object, bool>>>(existingValidator.Rules))
                    : new ManualValidatorService();
                
                // Add the new rule
                if (!validator.Rules.TryGetValue(typeof(T), out var list))
                {
                    list = new List<Func<object, bool>>();
                    validator.Rules[typeof(T)] = list;
                }
                list.Add(o => rule((T)o));
                
                return validator;
            });
        }
        else
        {
            // No existing registration, register a new one with just this rule
            services.AddSingleton<IManualValidatorService>(sp =>
            {
                var validator = new ManualValidatorService();
                if (!validator.Rules.TryGetValue(typeof(T), out var list))
                {
                    list = new List<Func<object, bool>>();
                    validator.Rules[typeof(T)] = list;
                }
                list.Add(o => rule((T)o));
                return validator;
            });
        }
        
        return services;
    }

    /// <summary>
    /// Register a configurable EntityIdProvider that allows custom selectors for different entity types.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure the provider with custom selectors</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddConfigurableEntityIdProvider(
        this IServiceCollection services,
        Action<ConfigurableEntityIdProvider> configure)
    {
        // Only register if no IEntityIdProvider is already registered
        var existingProviderDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IEntityIdProvider));
        if (existingProviderDescriptor == null)
        {
            var provider = new ConfigurableEntityIdProvider();
            configure(provider);
            services.AddSingleton<IEntityIdProvider>(provider);
        }
        return services;
    }

    /// <summary>
    /// Register a reflection-based EntityIdProvider that automatically discovers suitable string properties.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="propertyPriority">Optional custom property priority order</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddReflectionBasedEntityIdProvider(
        this IServiceCollection services,
        params string[] propertyPriority)
    {
        // Only register if no IEntityIdProvider is already registered
        var existingProviderDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IEntityIdProvider));
        if (existingProviderDescriptor == null)
        {
            var provider = propertyPriority.Length > 0 
                ? new ReflectionBasedEntityIdProvider(propertyPriority)
                : new ReflectionBasedEntityIdProvider();
            services.AddSingleton<IEntityIdProvider>(provider);
        }
        return services;
    }

    /// <summary>
    /// Register a default EntityIdProvider based on common conventions.
    /// This is a convenience method that sets up a ReflectionBasedEntityIdProvider with sensible defaults.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDefaultEntityIdProvider(this IServiceCollection services)
    {
        return services.AddReflectionBasedEntityIdProvider();
    }

    /// <summary>
    /// Helper method to find registered DbContext types in the service collection.
    /// </summary>
    internal static Type? FindRegisteredDbContextType(IServiceCollection services)
    {
        // Look for TheNannyDbContext first
        var nannyDbContextDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TheNannyDbContext));
        if (nannyDbContextDescriptor != null)
        {
            return typeof(TheNannyDbContext);
        }

        // Look for any DbContext subclass registration
        var dbContextDescriptor = services.FirstOrDefault(d => 
            d.ServiceType.IsSubclassOf(typeof(DbContext)) && 
            d.ServiceType != typeof(DbContext));
        
        return dbContextDescriptor?.ServiceType;
    }

    /// <summary>
    /// Convenience method to set up the complete ExampleLib validation stack with minimal configuration.
    /// Registers all core services including stores, validators, and providers with sensible defaults.
    /// This is the LEGACY method, use AddExampleLibValidation(string) instead.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddExampleLibValidationLegacy(
        this IServiceCollection services,
        Action<ExampleLibValidationBuilder>? configure = null)
    {
        ExampleLibValidationBuilder builder;
        if (configure != null)
        {
            builder = new ExampleLibValidationBuilder();
            configure(builder);
        }
        else
        {
            builder = new ExampleLibValidationBuilder(); // Use default settings
        }

        // Register core stores
        if (!services.Any(x => x.ServiceType == typeof(ISummarisationPlanStore)))
        {
            services.AddSingleton<ISummarisationPlanStore, InMemorySummarisationPlanStore>();
        }

        if (!services.Any(x => x.ServiceType == typeof(IValidationPlanStore)))
        {
            services.AddSingleton<IValidationPlanStore, InMemoryValidationPlanStore>();
        }

        // Register IApplicationNameProvider if not already registered
        if (!services.Any(x => x.ServiceType == typeof(IApplicationNameProvider)))
        {
            services.AddSingleton<IApplicationNameProvider>(sp => 
                new StaticApplicationNameProvider("ExampleLib-DefaultApp"));
        }

        // Register EntityIdProvider if not already registered
        if (!services.Any(x => x.ServiceType == typeof(IEntityIdProvider)))
        {
            services.AddDefaultEntityIdProvider();
        }

        // Register core validation services
        services.AddValidatorService();
        
        // Register save audit repository if not already registered
        if (!services.Any(x => x.ServiceType == typeof(ISaveAuditRepository)))
        {
            if (builder.PreferMongo)
            {
                services.AddScoped<ISaveAuditRepository>(sp =>
                {
                    var mongoClient = sp.GetService<IMongoClient>();
                    if (mongoClient == null)
                    {
                        throw new InvalidOperationException(
                            "No IMongoClient is registered. When using MongoDB with ExampleLib, " +
                            "you must register an IMongoClient using services.AddSingleton<IMongoClient>() " +
                            "or similar MongoDB configuration.");
                    }
                    return new MongoSaveAuditRepository(mongoClient);
                });
            }
            else
            {
                // Find what DbContext types are registered before building the provider
                var dbContextType = FindRegisteredDbContextType(services);
                
                // Use a deferred factory that resolves DbContext at runtime
                services.AddScoped<ISaveAuditRepository>(sp =>
                {
                    // First try TheNannyDbContext if available
                    var theNannyDbContext = sp.GetService<TheNannyDbContext>();
                    if (theNannyDbContext != null)
                    {
                        return new EfSaveAuditRepository(theNannyDbContext);
                    }
                    
                    // Use the found DbContext type
                    if (dbContextType != null)
                    {
                        var dbContext = (DbContext)sp.GetRequiredService(dbContextType);
                        return new EfSaveAuditRepository(dbContext);
                    }
                    
                    throw new InvalidOperationException(
                        "No DbContext is registered. When using Entity Framework with ExampleLib, " +
                        "you must register a DbContext (preferably TheNannyDbContext) using " +
                        "services.AddDbContext<TheNannyDbContext>() or similar.");
                });
            }
        }
        
        // Register IValidationService
        services.AddScoped<IValidationService>(sp => new ValidationService(
            sp.GetRequiredService<ISummarisationPlanStore>(),
            sp.GetRequiredService<ISaveAuditRepository>(),
            sp,
            sp.GetRequiredService<IApplicationNameProvider>(),
            sp.GetService<IEntityIdProvider>()));
        
        services.AddSingleton(typeof(ISummarisationValidator<>), typeof(SummarisationValidator<>));
        services.AddValidationRunner();

        return services;
    }
}

/// <summary>
/// Fluent configuration for entity-specific validation setup.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
public class ValidationConfiguration<TEntity> 
    where TEntity : class, IValidatable, IBaseEntity, IRootEntity
{
    private readonly IServiceCollection _services;
    
    public ValidationConfiguration(IServiceCollection services)
    {
        _services = services;
    }
    
    /// <summary>
    /// Add a summarisation plan with validation plan using the same metric.
    /// </summary>
    /// <param name="metricSelector">Function to select the metric from the entity</param>
    /// <param name="summaryThreshold">Threshold for summarisation validation</param>
    /// <param name="sequenceThreshold">Threshold for sequence validation (default: 10.0)</param>
    /// <param name="thresholdType">Type of threshold comparison (default: RawDifference)</param>
    /// <param name="strategy">Validation strategy (default: Average)</param>
    /// <returns>The configuration for chaining</returns>
    public ValidationConfiguration<TEntity> WithMetricValidation(
        Func<TEntity, decimal> metricSelector,
        decimal summaryThreshold,
        double sequenceThreshold = 10.0,
        ThresholdType thresholdType = ThresholdType.RawDifference,
        ValidationStrategy strategy = ValidationStrategy.Average)
    {
        // Store plan configurations for deferred registration
        var summaryPlanConfig = new SummarisationPlanConfiguration
        {
            EntityType = typeof(TEntity),
            MetricSelector = metricSelector,
            ThresholdType = thresholdType,
            ThresholdValue = summaryThreshold
        };
        
        var validationPlanConfig = new ValidationPlanConfiguration
        {
            EntityType = typeof(TEntity),
            Threshold = sequenceThreshold,
            Strategy = strategy
        };
        
        // Add to a configuration list that will be processed when the service provider is built
        AddPlanConfiguration(summaryPlanConfig, validationPlanConfig);
        
        return this;
    }
    
    /// <summary>
    /// Add manual validation rules.
    /// </summary>
    /// <param name="rules">Validation rules to add</param>
    /// <returns>The configuration for chaining</returns>
    public ValidationConfiguration<TEntity> WithRules(params Func<TEntity, bool>[] rules)
    {
        foreach (var rule in rules)
        {
            _services.AddValidatorRule(rule);
        }
        return this;
    }
    
    /// <summary>
    /// Configure custom entity ID extraction for SaveAudit records.
    /// </summary>
    /// <param name="selector">Function to extract entity ID</param>
    /// <returns>The configuration for chaining</returns>
    public ValidationConfiguration<TEntity> WithEntityIdSelector(Func<TEntity, string> selector)
    {
        // Remove existing EntityIdProvider if it exists and add a configurable one
        var existingProvider = _services.FirstOrDefault(d => d.ServiceType == typeof(IEntityIdProvider));
        if (existingProvider != null)
        {
            _services.Remove(existingProvider);
        }
        
        _services.AddSingleton<IEntityIdProvider>(sp =>
        {
            var provider = new ConfigurableEntityIdProvider();
            provider.RegisterSelector(selector);
            return provider;
        });
        
        return this;
    }
    
    private void AddPlanConfiguration(SummarisationPlanConfiguration summaryConfig, ValidationPlanConfiguration validationConfig)
    {
        // Get or create the plan configuration registry
        var registry = GetOrCreatePlanRegistry();
        
        // Add configurations to registry
        registry.AddSummarisationPlan(summaryConfig);
        registry.AddValidationPlan(validationConfig);
        
        // Update the store registrations
        UpdateStoreRegistrations();
    }
    
    private ValidationPlanRegistry GetOrCreatePlanRegistry()
    {
        // Check if registry already exists in services
        var registryDescriptor = _services.FirstOrDefault(s => s.ServiceType == typeof(ValidationPlanRegistry));
        if (registryDescriptor?.ImplementationInstance is ValidationPlanRegistry registry)
        {
            return registry;
        }
        
        // Create new registry and register it
        registry = new ValidationPlanRegistry();
        if (registryDescriptor != null)
        {
            _services.Remove(registryDescriptor);
        }
        _services.AddSingleton(registry);
        
        return registry;
    }
    
    private void UpdateStoreRegistrations()
    {
        var registry = GetOrCreatePlanRegistry();
        
        // Remove existing store registrations
        var existingSummaryStore = _services.FirstOrDefault(d => d.ServiceType == typeof(ISummarisationPlanStore));
        var existingValidationStore = _services.FirstOrDefault(d => d.ServiceType == typeof(IValidationPlanStore));
        
        if (existingSummaryStore != null)
        {
            _services.Remove(existingSummaryStore);
        }
        if (existingValidationStore != null)
        {
            _services.Remove(existingValidationStore);
        }
        
        // Register new stores that use the registry
        _services.AddSingleton<ISummarisationPlanStore>(sp =>
        {
            var planRegistry = sp.GetRequiredService<ValidationPlanRegistry>();
            var store = new InMemorySummarisationPlanStore();
            
            foreach (var config in planRegistry.SummarisationPlans)
            {
                // Create the generic SummarisationPlan<T> using reflection
                var planType = typeof(SummarisationPlan<>).MakeGenericType(config.EntityType);
                var plan = Activator.CreateInstance(planType, config.MetricSelector, config.ThresholdType, config.ThresholdValue);
                
                if (plan != null)
                {
                    var addPlanMethod = typeof(InMemorySummarisationPlanStore).GetMethod("AddPlan");
                    var genericMethod = addPlanMethod?.MakeGenericMethod(config.EntityType);
                    genericMethod?.Invoke(store, new object[] { plan });
                }
            }
            
            return store;
        });
        
        _services.AddSingleton<IValidationPlanStore>(sp =>
        {
            var planRegistry = sp.GetRequiredService<ValidationPlanRegistry>();
            var store = new InMemoryValidationPlanStore();
            
            foreach (var config in planRegistry.ValidationPlans)
            {
                var plan = new ValidationPlan(config.EntityType, config.Threshold, config.Strategy);
                store.AddPlan(plan);
            }
            
            return store;
        });
    }
}

/// <summary>
/// Configuration for a summarisation plan.
/// </summary>
public class SummarisationPlanConfiguration
{
    public Type EntityType { get; set; } = null!;
    public object MetricSelector { get; set; } = null!;
    public ThresholdType ThresholdType { get; set; }
    public decimal ThresholdValue { get; set; }
}

/// <summary>
/// Configuration for a validation plan.
/// </summary>
public class ValidationPlanConfiguration
{
    public Type EntityType { get; set; } = null!;
    public double Threshold { get; set; }
    public ValidationStrategy Strategy { get; set; }
}

/// <summary>
/// Registry for collecting plan configurations before service provider is built.
/// </summary>
public class ValidationPlanRegistry
{
    public List<SummarisationPlanConfiguration> SummarisationPlans { get; } = new();
    public List<ValidationPlanConfiguration> ValidationPlans { get; } = new();
    
    public void AddSummarisationPlan(SummarisationPlanConfiguration config)
    {
        SummarisationPlans.Add(config);
    }
    
    public void AddValidationPlan(ValidationPlanConfiguration config)
    {
        ValidationPlans.Add(config);
    }
}

/// <summary>
/// Builder for configuring ExampleLib validation services.
/// </summary>
public class ExampleLibValidationBuilder
{
    /// <summary>
    /// Whether to prefer MongoDB implementations over Entity Framework where both are available.
    /// </summary>
    public bool PreferMongo { get; set; } = false;

    /// <summary>
    /// Configure the builder to prefer MongoDB implementations.
    /// </summary>
    /// <returns>The builder for chaining</returns>
    public ExampleLibValidationBuilder UseMongo()
    {
        PreferMongo = true;
        return this;
    }

    /// <summary>
    /// Configure the builder to prefer Entity Framework implementations.
    /// </summary>
    /// <returns>The builder for chaining</returns>
    public ExampleLibValidationBuilder UseEntityFramework()
    {
        PreferMongo = false;
        return this;
    }
}