using ExampleLib.Domain;
using MassTransit;
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

        services.AddMassTransit(x =>
        {
            x.AddConsumer<SaveValidationConsumer<T>>();
            x.UsingInMemory((ctx, cfg) =>
            {
                cfg.ReceiveEndpoint("save_requests_queue", e =>
                {
                    e.ConfigureConsumer<SaveValidationConsumer<T>>(ctx);
                });
            });
        });

        services.AddScoped<IEntityRepository<T>, EventPublishingRepository<T>>();
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
}
