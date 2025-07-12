using ExampleLib.Domain;
using ExampleData;
using System.Collections.Generic;
using System.Reflection;
using MassTransit;
using OpenTelemetry.Extensions.Hosting;
using OpenTelemetry.Trace;
using Serilog;
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

        services.AddLogging(b => b.AddSerilog());
        services.AddOpenTelemetry().WithTracing(b => b.AddSource("MassTransit"));

        services.AddMassTransit(x =>
        {
            x.AddConsumer<SaveValidationConsumer<T>>();
            x.UsingInMemory((ctx, cfg) =>
            {
                cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromMilliseconds(200)));
                cfg.ConnectReceiveObserver(new SerilogReceiveObserver(Log.Logger));

                cfg.ReceiveEndpoint("save_requests_queue", e =>
                {
                    e.UseInMemoryOutbox();
                    e.ConfigureConsumer<SaveValidationConsumer<T>>(ctx);
                });
            });
        });

        services.AddScoped<IEntityRepository<T>, EventPublishingRepository<T>>();
        return services;
    }

    /// <summary>
    /// Register the <see cref="SaveCommitConsumer{T}"/> to record commit audits.
    /// MassTransit is configured with a dedicated receive endpoint.
    /// </summary>
    public static IServiceCollection AddSaveCommit<T>(this IServiceCollection services)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<SaveValidationConsumer<T>>();
            x.AddConsumer<SaveCommitConsumer<T>>();
            x.UsingInMemory((ctx, cfg) =>
            {
                cfg.ReceiveEndpoint("save_requests_queue", e =>
                {
                    e.ConfigureConsumer<SaveValidationConsumer<T>>(ctx);
                });
                cfg.ReceiveEndpoint("save_commits_queue", e =>
                {
                    e.ConfigureConsumer<SaveCommitConsumer<T>>(ctx);
                });
            });
        });
        return services;
    }

    /// <summary>
    /// Register the services required to validate delete requests for <typeparamref name="T"/>.
    /// </summary>
    public static IServiceCollection AddDeleteValidation<T>(this IServiceCollection services)
    {
        services.AddValidatorService();
        services.AddMassTransit(x =>
        {
            x.AddConsumer<DeleteValidationConsumer<T>>();
            x.UsingInMemory((ctx, cfg) =>
            {
                cfg.ReceiveEndpoint("delete_requests_queue", e =>
                {
                    e.ConfigureConsumer<DeleteValidationConsumer<T>>(ctx);
                });
            });
        });
        return services;
    }

    /// <summary>
    /// Register the services required to commit deletes for <typeparamref name="T"/>.
    /// </summary>
    public static IServiceCollection AddDeleteCommit<T>(this IServiceCollection services)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<DeleteCommitConsumer<T>>();
            x.UsingInMemory((ctx, cfg) =>
            {
                cfg.ReceiveEndpoint("delete_commit_queue", e =>
                {
                    e.ConfigureConsumer<DeleteCommitConsumer<T>>(ctx);
                });
            });
        });
        return services;
    }

    /// <summary>
    /// Register EF Core repositories and validation services in one call.
    /// A default summarisation plan is created for <typeparamref name="T"/>.
    /// </summary>
    public static IServiceCollection AddValidationForEfCore<T, TContext>(
        this IServiceCollection services,
        string connectionString,
        Func<T, decimal>? metricSelector = null,
        ThresholdType thresholdType = ThresholdType.PercentChange,
        decimal thresholdValue = 0.1m)
        where TContext : YourDbContext
    {
        services.SetupDatabase<TContext>(connectionString);
        services.AddSaveValidation<T>(metricSelector, thresholdType, thresholdValue);
        services.AddScoped<ISaveAuditRepository, EfSaveAuditRepository>();
        return services;
    }

    /// <summary>
    /// Register MongoDB repositories and validation services in one call.
    /// A default summarisation plan is created for <typeparamref name="T"/>.
    /// </summary>
    public static IServiceCollection AddValidationForMongo<T>(
        this IServiceCollection services,
        string connectionString,
        string databaseName,
        Func<T, decimal>? metricSelector = null,
        ThresholdType thresholdType = ThresholdType.PercentChange,
        decimal thresholdValue = 0.1m)
    {
        services.SetupMongoDatabase(connectionString, databaseName);
        services.AddSaveValidation<T>(metricSelector, thresholdType, thresholdValue);
        services.AddScoped<ISaveAuditRepository, MongoSaveAuditRepository>();
        return services;
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
                    .Invoke(null, new object[] { services, selector, flow.ThresholdType, flow.ThresholdValue });
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
