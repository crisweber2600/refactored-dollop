using System.Reflection;
using ExampleLib.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Service registration helpers for the summarisation validation workflow.
/// </summary>
public static class ServiceCollectionExtensions
{
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
