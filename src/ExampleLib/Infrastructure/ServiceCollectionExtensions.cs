using ExampleLib.Domain;
using System.Collections.Generic;
using System.Reflection;
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
        decimal thresholdValue = 0.1m)
    {
        services.AddSingleton(typeof(ISummarisationValidator<>), typeof(SummarisationValidator<>));
        services.AddSingleton<ISaveAuditRepository, InMemorySaveAuditRepository>();
        services.AddSingleton<ISummarisationPlanStore>(sp =>
        {
            var store = new InMemorySummarisationPlanStore();
            Func<T, decimal> selector = metricSelector ?? DefaultSelector<T>;
            store.AddPlan(new SummarisationPlan<T>(selector, thresholdType, thresholdValue));
            return store;
        });

        services.AddScoped<IValidationService, ValidationService>();
        return services;
    }

    /// <summary>
    /// Placeholder for backwards compatibility. Currently performs no registration.
    /// </summary>
    public static IServiceCollection AddSaveCommit<T>(this IServiceCollection services)
    {
        return services;
    }

    /// <summary>
    /// Register the services required to validate delete requests for <typeparamref name="T"/>.
    /// </summary>
    public static IServiceCollection AddDeleteValidation<T>(this IServiceCollection services)
    {
        services.AddValidatorService();
        return services;
    }

    /// <summary>
    /// Register the services required to commit deletes for <typeparamref name="T"/>.
    /// </summary>
    public static IServiceCollection AddDeleteCommit<T>(this IServiceCollection services)
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
        decimal thresholdValue = 0.1m)
    {
        var builder = new SetupValidationBuilder();
        configure(builder);
        builder.Apply(services);

        services.AddSaveValidation<T>(metricSelector, thresholdType, thresholdValue);
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

    private static readonly IDictionary<Type, List<Func<object, bool>>> _validatorRules = new Dictionary<Type, List<Func<object, bool>>>();

    /// <summary>
    /// Register <see cref="ManualValidatorService"/> and the rule dictionary as singletons.
    /// </summary>
    public static IServiceCollection AddValidatorService(this IServiceCollection services)
    {
        services.AddSingleton(_validatorRules);
        services.AddSingleton<IManualValidatorService>(new ManualValidatorService(_validatorRules));
        return services;
    }

    /// <summary>
    /// Add a manual validation rule for the specified type.
    /// </summary>
    public static IServiceCollection AddValidatorRule<T>(this IServiceCollection services, Func<T, bool> rule)
    {
        if (!_validatorRules.TryGetValue(typeof(T), out var list))
        {
            list = new List<Func<object, bool>>();
            _validatorRules[typeof(T)] = list;
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
}
