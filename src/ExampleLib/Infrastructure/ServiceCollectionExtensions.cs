using System.Reflection;
using ExampleLib.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Service registration helpers for the summarisation validation workflow.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register the services required to validate saves of <typeparamref name="T"/>.
    /// A default summarisation plan is configured using the provided options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="metricSelector">Function selecting the metric from the entity. Defaults to Id if present or 0.</param>
    /// <param name="thresholdType">The threshold comparison type.</param>
    /// <param name="thresholdValue">The allowed threshold value.</param>
    public static IServiceCollection AddSaveValidation<T>(
        this IServiceCollection services,
        Func<T, decimal>? metricSelector = null,
        ThresholdType thresholdType = ThresholdType.PercentChange,
        decimal thresholdValue = 0.1m,
        params Func<T, bool>[] manualRules)
        where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        services.AddSingleton(typeof(ISummarisationValidator<>), typeof(SummarisationValidator<>));
        services.AddSingleton<ISaveAuditRepository, InMemorySaveAuditRepository>();
        services.AddSingleton<ISummarisationPlanStore>(sp =>
        {
            var store = new InMemorySummarisationPlanStore();
            Func<T, decimal> selector = metricSelector ?? DefaultSelector;
            store.AddPlan(new SummarisationPlan<T>(selector, thresholdType, thresholdValue));
            return store;
        });

        services.AddValidatorService();
        foreach (var rule in manualRules)
        {
            services.AddValidatorRule(rule);
        }

        services.AddScoped<IValidationService, ValidationService>();
        services.AddScoped<IValidationRunner, ValidationRunner>();
        return services;
    }

    /// <summary>
    /// Placeholder for backwards compatibility. Currently performs no registration.
    /// </summary>
    public static IServiceCollection AddSaveCommit<T>(this IServiceCollection services)
        where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        return services;
    }

    /// <summary>
    /// Register the services required to validate delete requests for <typeparamref name="T"/>.
    /// </summary>
    public static IServiceCollection AddDeleteValidation<T>(this IServiceCollection services)
        where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        services.AddValidatorService();
        return services;
    }

    /// <summary>
    /// Register the services required to commit deletes for <typeparamref name="T"/>.
    /// </summary>
    public static IServiceCollection AddDeleteCommit<T>(this IServiceCollection services)
        where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        return services;
    }

    /// <summary>
    /// Convenience helper combining <see cref="SetupValidation"/> and
    /// <see cref="AddSaveValidation{T}"/>. The builder action configures the
    /// data layer while a default summarisation plan is registered for
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action configuring the setup builder.</param>
    /// <param name="metricSelector">Metric selector for the plan.</param>
    /// <param name="thresholdType">Threshold comparison type.</param>
    /// <param name="thresholdValue">Allowed threshold value.</param>
    public static IServiceCollection AddSetupValidation<T>(
        this IServiceCollection services,
        Action<SetupValidationBuilder> configure,
        Func<T, decimal>? metricSelector = null,
        ThresholdType thresholdType = ThresholdType.PercentChange,
        decimal thresholdValue = 0.1m,
        params Func<T, bool>[] manualRules)
        where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        var builder = new SetupValidationBuilder();
        configure(builder);
        builder.Apply(services);

        services.AddSaveValidation<T>(metricSelector, thresholdType, thresholdValue, manualRules);
        if (builder.UsesMongo)
        {
            services.AddScoped<ISaveAuditRepository, MongoSaveAuditRepository>();
        }
        else
        {
            services.AddScoped<ISaveAuditRepository, EfSaveAuditRepository>();
        }
        return services;
    }

    /// <summary>
    /// Configure validation services using a fluent <see cref="SetupValidationBuilder"/>.
    /// Recorded steps are applied to the service collection after <paramref name="configure"/> executes.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action configuring the <see cref="SetupValidationBuilder"/>.</param>
    public static IServiceCollection SetupValidation(
        this IServiceCollection services,
        Action<SetupValidationBuilder> configure)
    {
        var builder = new SetupValidationBuilder();
        configure(builder);
        return builder.Apply(services);
    }

    private static decimal DefaultSelector<T>(T entity)
    {
        var prop = typeof(T).GetProperty("Id");
        var value = prop?.GetValue(entity);
        if (value != null && decimal.TryParse(value.ToString(), out var result))
        {
            return result;
        }
        return 0m;
    }

    private static Delegate BuildMetricSelector(Type entityType, string property)
    {
        var method = typeof(ServiceCollectionExtensions)
            .GetMethod(nameof(CreateSelector), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(entityType);
        return (Delegate)method.Invoke(null, new object[] { property })!;
    }

    private static Func<T, decimal> CreateSelector<T>(string property)
    {
        var prop = typeof(T).GetProperty(property);
        return entity =>
        {
            var value = prop?.GetValue(entity);
            if (value != null && decimal.TryParse(value.ToString(), out var result))
                return result;
            return 0m;
        };
    }

    private static ManualValidatorService? _manualValidator;

    /// <summary>
    /// Register <see cref="ManualValidatorService"/> and the rule dictionary as singletons.
    /// </summary>
    public static IServiceCollection AddValidatorService(this IServiceCollection services)
    {
        _manualValidator ??= new ManualValidatorService();
        services.AddSingleton<IManualValidatorService>(_manualValidator);
        return services;
    }

    /// <summary>
    /// Register <see cref="ValidationRunner"/> for executing all validations including sequence validation.
    /// </summary>
    public static IServiceCollection AddValidationRunner(this IServiceCollection services)
    {
        services.AddScoped<IValidationRunner>(sp => new ValidationRunner(
            sp.GetRequiredService<IValidationService>(),
            sp.GetRequiredService<IManualValidatorService>(),
            sp));
        return services;
    }

    /// <summary>
    /// Add a manual validation rule for the specified type.
    /// </summary>
    public static IServiceCollection AddValidatorRule<T>(this IServiceCollection services, Func<T, bool> rule)
        where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        _manualValidator ??= new ManualValidatorService();
        if (!_manualValidator.Rules.TryGetValue(typeof(T), out var list))
        {
            list = new List<Func<object, bool>>();
            _manualValidator.Rules[typeof(T)] = list;
        }
        list.Add(o => rule((T)o));
        return services;
    }

    /// <summary>
    /// Register validation flows based on external configuration.
    /// </summary>
    public static IServiceCollection AddValidationFlows(this IServiceCollection services, ValidationFlowOptions options)
    {
        foreach (var flow in options.Flows)
        {
            var type = Type.GetType(flow.Type);
            if (type == null) continue;
            if (flow.SaveValidation)
            {
                var selector = flow.MetricProperty == null ? null : BuildMetricSelector(type, flow.MetricProperty);
                typeof(ServiceCollectionExtensions)
                    .GetMethod(nameof(AddSaveValidation))!
                    .MakeGenericMethod(type)
                    .Invoke(null, new object?[] { services, selector, flow.ThresholdType, flow.ThresholdValue });
            }
            if (flow.SaveCommit)
            {
                typeof(ServiceCollectionExtensions)
                    .GetMethod(nameof(AddSaveCommit))!
                    .MakeGenericMethod(type)
                    .Invoke(null, new object[] { services });
            }
            if (flow.DeleteValidation)
            {
                typeof(ServiceCollectionExtensions)
                    .GetMethod(nameof(AddDeleteValidation))!
                    .MakeGenericMethod(type)
                    .Invoke(null, new object[] { services });
            }
            if (flow.DeleteCommit)
            {
                typeof(ServiceCollectionExtensions)
                    .GetMethod(nameof(AddDeleteCommit))!
                    .MakeGenericMethod(type)
                    .Invoke(null, new object[] { services });
            }
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
        var provider = new ConfigurableEntityIdProvider();
        configure(provider);
        services.AddSingleton<IEntityIdProvider>(provider);
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
        var provider = propertyPriority.Length > 0 
            ? new ReflectionBasedEntityIdProvider(propertyPriority)
            : new ReflectionBasedEntityIdProvider();
        services.AddSingleton<IEntityIdProvider>(provider);
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
    /// Convenience method to set up the complete ExampleLib validation stack with minimal configuration.
    /// Registers all core services including stores, validators, and providers with sensible defaults.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddExampleLibValidation(
        this IServiceCollection services,
        Action<ExampleLibValidationBuilder>? configure = null)
    {
        var builder = new ExampleLibValidationBuilder();
        configure?.Invoke(builder);

        // Register core stores
        if (!services.Any(x => x.ServiceType == typeof(ISummarisationPlanStore)))
        {
            services.AddSingleton<ISummarisationPlanStore, InMemorySummarisationPlanStore>();
        }

        if (!services.Any(x => x.ServiceType == typeof(IValidationPlanStore)))
        {
            services.AddSingleton<IValidationPlanStore, InMemoryValidationPlanStore>();
        }

        // Register core validation services
        services.AddValidatorService();
        services.AddScoped<IValidationService, ValidationService>();
        services.AddSingleton(typeof(ISummarisationValidator<>), typeof(SummarisationValidator<>));
        services.AddValidationRunner();

        // Register EntityIdProvider if not already registered
        if (!services.Any(x => x.ServiceType == typeof(IEntityIdProvider)))
        {
            services.AddDefaultEntityIdProvider();
        }

        // Register save audit repository if not already registered
        if (!services.Any(x => x.ServiceType == typeof(ISaveAuditRepository)))
        {
            if (builder.PreferMongo)
            {
                services.AddScoped<ISaveAuditRepository, MongoSaveAuditRepository>();
            }
            else
            {
                services.AddScoped<ISaveAuditRepository, EfSaveAuditRepository>();
            }
        }

        return services;
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
