namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Extension methods for registering pipeline services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the pipeline using the default <see cref="SummaryDbContext"/>.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="dbCfg">Action configuring the database context.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddMetricsPipeline(this IServiceCollection services, Action<DbContextOptionsBuilder> dbCfg)
        => services.AddMetricsPipeline<SummaryDbContext>(dbCfg);

    /// <summary>
    /// Registers the pipeline using a custom context type.
    /// </summary>
    /// <typeparam name="TContext">The context implementing <see cref="SummaryDbContext"/>.</typeparam>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="dbCfg">Action configuring the database context.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddMetricsPipeline<TContext>(this IServiceCollection services, Action<DbContextOptionsBuilder> dbCfg)
        where TContext : SummaryDbContext
    {
        services.AddDbContext<SummaryDbContext, TContext>(dbCfg);
        // Use a scoped lifetime for the in-memory gather service so that
        // the orchestrator and step definitions share the same instance
        // within a single scenario while avoiding cross-scenario state.
        services.AddScoped<IGatherService, InMemoryGatherService>();
        services.AddScoped<IWorkerService, InMemoryGatherService>();
        services.AddTransient<ISummarizationService, InMemorySummarizationService>();
        services.AddTransient<IValidationService, ThresholdValidationService>();
        services.AddTransient<ICommitService, EfCommitService>();
        services.AddTransient<IDiscardHandler, LoggingDiscardHandler>();
        services.AddTransient<ISummaryRepository, EfSummaryRepository>();
        services.AddTransient<IPipelineOrchestrator, PipelineOrchestrator>();
        return services;
    }
}
