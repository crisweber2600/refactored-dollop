using ExampleLib.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Fluent configuration builder for ExampleLib validation system.
/// </summary>
public class ExampleLibConfigurationBuilder
{
    private readonly IServiceCollection _services;
    private readonly ExampleLibOptions _options;
    private readonly List<object> _summarisationPlans = new();
    private readonly List<ValidationPlan> _validationPlans = new();

    internal ExampleLibConfigurationBuilder(IServiceCollection services, ExampleLibOptions options)
    {
        _services = services;
        _options = options;
    }

    /// <summary>
    /// Configure the application name used in SaveAudit records.
    /// </summary>
    /// <param name="applicationName">The application name</param>
    /// <returns>The builder for chaining</returns>
    public ExampleLibConfigurationBuilder WithApplicationName(string applicationName)
    {
        _options.ApplicationName = applicationName;
        return this;
    }

    /// <summary>
    /// Configure ExampleLib to use MongoDB as the preferred data store.
    /// </summary>
    /// <param name="configure">Optional MongoDB-specific configuration</param>
    /// <returns>The builder for chaining</returns>
    public ExampleLibConfigurationBuilder UseMongoDb(Action<MongoDbOptions>? configure = null)
    {
        _options.UseMongoDb = true;
        configure?.Invoke(_options.MongoDb);
        return this;
    }

    /// <summary>
    /// Configure ExampleLib to use Entity Framework as the preferred data store.
    /// </summary>
    /// <param name="configure">Optional Entity Framework-specific configuration</param>
    /// <returns>The builder for chaining</returns>
    public ExampleLibConfigurationBuilder UseEntityFramework(Action<EntityFrameworkOptions>? configure = null)
    {
        _options.UseMongoDb = false;
        configure?.Invoke(_options.EntityFramework);
        return this;
    }

    /// <summary>
    /// Configure the EntityIdProvider to use reflection-based property discovery.
    /// </summary>
    /// <param name="propertyPriority">Optional custom property priority order</param>
    /// <returns>The builder for chaining</returns>
    public ExampleLibConfigurationBuilder WithReflectionBasedEntityIds(params string[] propertyPriority)
    {
        _options.EntityIdProvider.Type = EntityIdProviderType.Reflection;
        if (propertyPriority.Length > 0)
        {
            _options.EntityIdProvider.PropertyPriority = propertyPriority;
        }
        return this;
    }

    /// <summary>
    /// Configure the EntityIdProvider to use configurable selectors.
    /// </summary>
    /// <param name="configure">Action to configure custom selectors</param>
    /// <returns>The builder for chaining</returns>
    public ExampleLibConfigurationBuilder WithConfigurableEntityIds(Action<ConfigurableEntityIdProvider> configure)
    {
        _options.EntityIdProvider.Type = EntityIdProviderType.Configurable;
        _services.AddConfigurableEntityIdProvider(configure);
        return this;
    }

    /// <summary>
    /// Configure default threshold settings for summarisation validation.
    /// </summary>
    /// <param name="thresholdType">The threshold comparison type</param>
    /// <param name="thresholdValue">The threshold value</param>
    /// <returns>The builder for chaining</returns>
    public ExampleLibConfigurationBuilder WithDefaultThresholds(ThresholdType thresholdType, decimal thresholdValue)
    {
        _options.DefaultThresholdType = thresholdType;
        _options.DefaultThresholdValue = thresholdValue;
        return this;
    }

    /// <summary>
    /// Add a summarisation plan for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="metricSelector">Function to select the metric from the entity</param>
    /// <param name="thresholdType">The threshold comparison type</param>
    /// <param name="thresholdValue">The threshold value</param>
    /// <returns>The builder for chaining</returns>
    public ExampleLibConfigurationBuilder AddSummarisationPlan<T>(
        Func<T, decimal> metricSelector,
        ThresholdType? thresholdType = null,
        decimal? thresholdValue = null)
        where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        var plan = new SummarisationPlan<T>(
            metricSelector,
            thresholdType ?? _options.DefaultThresholdType,
            thresholdValue ?? _options.DefaultThresholdValue);

        _summarisationPlans.Add(plan);
        return this;
    }

    /// <summary>
    /// Add a validation plan for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="threshold">The validation threshold</param>
    /// <param name="strategy">The validation strategy</param>
    /// <returns>The builder for chaining</returns>
    public ExampleLibConfigurationBuilder AddValidationPlan<T>(
        double threshold,
        ValidationStrategy strategy = ValidationStrategy.Count)
        where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        var plan = new ValidationPlan(typeof(T), threshold, strategy);
        _validationPlans.Add(plan);
        return this;
    }

    /// <summary>
    /// Add manual validation rules for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="rules">The validation rules</param>
    /// <returns>The builder for chaining</returns>
    public ExampleLibConfigurationBuilder AddValidationRules<T>(params Func<T, bool>[] rules)
        where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        foreach (var rule in rules)
        {
            _services.AddValidatorRule(rule);
        }
        return this;
    }

    /// <summary>
    /// Complete the configuration and register all services.
    /// </summary>
    /// <returns>The service collection for continued configuration</returns>
    public IServiceCollection Build()
    {
        // Register the application name provider
        _services.AddSingleton<IApplicationNameProvider>(
            new StaticApplicationNameProvider(_options.ApplicationName));

        // Register the appropriate EntityIdProvider if not already registered
        if (!_services.Any(x => x.ServiceType == typeof(IEntityIdProvider)))
        {
            switch (_options.EntityIdProvider.Type)
            {
                case EntityIdProviderType.Reflection:
                    _services.AddReflectionBasedEntityIdProvider(_options.EntityIdProvider.PropertyPriority);
                    break;
                case EntityIdProviderType.Configurable:
                    // Should have been registered in WithConfigurableEntityIds
                    break;
                case EntityIdProviderType.Default:
                default:
                    _services.AddDefaultEntityIdProvider();
                    break;
            }
        }

        // Replace the ISummarisationPlanStore registration with one that includes our plans
        var existingStoreDescriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(ISummarisationPlanStore));
        if (existingStoreDescriptor != null)
        {
            _services.Remove(existingStoreDescriptor);
        }

        _services.AddSingleton<ISummarisationPlanStore>(sp =>
        {
            var store = new InMemorySummarisationPlanStore();
            
            // Add all the plans that were configured through the fluent API
            foreach (var plan in _summarisationPlans)
            {
                var addPlanMethod = typeof(InMemorySummarisationPlanStore).GetMethod(nameof(InMemorySummarisationPlanStore.AddPlan));
                var planType = plan.GetType();
                var entityType = planType.GetGenericArguments()[0];
                var genericAddPlan = addPlanMethod!.MakeGenericMethod(entityType);
                genericAddPlan.Invoke(store, new[] { plan });
            }
            
            return store;
        });

        // Replace the IValidationPlanStore registration with one that includes our plans
        var existingValidationStoreDescriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(IValidationPlanStore));
        if (existingValidationStoreDescriptor != null)
        {
            _services.Remove(existingValidationStoreDescriptor);
        }

        _services.AddSingleton<IValidationPlanStore>(sp =>
        {
            var store = new InMemoryValidationPlanStore();
            
            // Add all the validation plans that were configured through the fluent API
            foreach (var plan in _validationPlans)
            {
                var addPlanMethod = typeof(InMemoryValidationPlanStore).GetMethod(nameof(InMemoryValidationPlanStore.AddPlan));
                var genericAddPlan = addPlanMethod!.MakeGenericMethod(plan.EntityType);
                genericAddPlan.Invoke(store, new[] { plan });
            }
            
            return store;
        });

        return _services;
    }
}

/// <summary>
/// Extension methods for configuring ExampleLib with fluent syntax.
/// </summary>
public static class ExampleLibFluentConfigurationExtensions
{
    /// <summary>
    /// Configure ExampleLib validation system using fluent syntax.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection ConfigureExampleLib(
        this IServiceCollection services,
        Action<ExampleLibConfigurationBuilder> configure)
    {
        var options = new ExampleLibOptions();
        var builder = new ExampleLibConfigurationBuilder(services, options);

        // Register core validation services first
        services.AddExampleLibValidation(validationBuilder =>
        {
            if (options.UseMongoDb)
                validationBuilder.UseMongo();
            else
                validationBuilder.UseEntityFramework();
        });

        configure(builder);
        return builder.Build();
    }

    /// <summary>
    /// Configure ExampleLib validation system using configuration file.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <param name="sectionName">Configuration section name (default: "ExampleLib")</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection ConfigureExampleLib(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "ExampleLib")
    {
        var options = new ExampleLibOptions();
        configuration.GetSection(sectionName).Bind(options);

        return services.ConfigureExampleLib(builder =>
        {
            builder.WithApplicationName(options.ApplicationName);

            if (options.UseMongoDb)
            {
                builder.UseMongoDb(mongo =>
                {
                    mongo.DefaultDatabaseName = options.MongoDb.DefaultDatabaseName;
                });
            }
            else
            {
                builder.UseEntityFramework();
            }

            builder.WithDefaultThresholds(options.DefaultThresholdType, options.DefaultThresholdValue);

            switch (options.EntityIdProvider.Type)
            {
                case EntityIdProviderType.Reflection:
                    builder.WithReflectionBasedEntityIds(options.EntityIdProvider.PropertyPriority);
                    break;
                // Other types would need additional configuration
            }
        });
    }
}