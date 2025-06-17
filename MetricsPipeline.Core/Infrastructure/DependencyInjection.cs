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
    public static IServiceCollection AddMetricsPipeline(this IServiceCollection services, Action<DbContextOptionsBuilder> dbCfg, PipelineMode mode = PipelineMode.InMemory)
        => services.AddMetricsPipeline<SummaryDbContext>(dbCfg, mode);

    /// <summary>
    /// Registers the pipeline using a custom context type.
    /// </summary>
    /// <typeparam name="TContext">The context implementing <see cref="SummaryDbContext"/>.</typeparam>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="dbCfg">Action configuring the database context.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddMetricsPipeline<TContext>(this IServiceCollection services, Action<DbContextOptionsBuilder> dbCfg, PipelineMode mode = PipelineMode.InMemory)
        where TContext : SummaryDbContext
    {
        services.AddDbContext<SummaryDbContext, TContext>(dbCfg);

        if (mode == PipelineMode.Http)
        {
            services.AddTransient<IGatherService, HttpGatherService>();
            services.AddTransient<IWorkerService, HttpWorkerService>();
        }
        else
        {
            // Use a single scoped instance of the gather service so both IGatherService
            // and IWorkerService resolve to the same object within a scenario.
            services.AddScoped<InMemoryGatherService>();
            services.AddScoped<IGatherService>(sp => sp.GetRequiredService<InMemoryGatherService>());
            services.AddScoped<IWorkerService>(sp => sp.GetRequiredService<InMemoryGatherService>());
        }
        services.AddTransient<ISummarizationService, InMemorySummarizationService>();
        services.AddTransient<IValidationService, ThresholdValidationService>();
        services.AddTransient<ICommitService, EfCommitService>();
        services.AddTransient<IDiscardHandler, LoggingDiscardHandler>();
        services.AddTransient<ISummaryRepository, EfSummaryRepository>();
        services.AddTransient<IPipelineOrchestrator, PipelineOrchestrator>();
        return services;
    }
}
