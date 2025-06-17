namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Net.Http;

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
    /// <param name="configure">Optional action to configure additional services.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddMetricsPipeline(this IServiceCollection services, Action<DbContextOptionsBuilder> dbCfg, Action<MetricsPipelineOptions>? configure = null)
        => services.AddMetricsPipeline<SummaryDbContext>(dbCfg, configure);

    /// <summary>
    /// Registers the pipeline using the default context and specifies a hosted worker type.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="workerType">Worker type implementing <see cref="IHostedService"/>.</param>
    /// <param name="dbCfg">Action configuring the database context.</param>
    /// <param name="configure">Optional action to configure additional services.</param>
    public static IServiceCollection AddMetricsPipeline(this IServiceCollection services, Type workerType, Action<DbContextOptionsBuilder> dbCfg, Action<MetricsPipelineOptions>? configure = null)
        => services.AddMetricsPipeline<SummaryDbContext>(dbCfg, o => { o.WorkerType = workerType; configure?.Invoke(o); });

    /// <summary>
    /// Registers the pipeline using a custom context type and hosted worker.
    /// </summary>
    /// <typeparam name="TContext">Database context implementation.</typeparam>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="workerType">Worker type implementing <see cref="IHostedService"/>.</param>
    /// <param name="dbCfg">Action configuring the database context.</param>
    /// <param name="configure">Optional action to configure additional services.</param>
    public static IServiceCollection AddMetricsPipeline<TContext>(this IServiceCollection services, Type workerType, Action<DbContextOptionsBuilder> dbCfg, Action<MetricsPipelineOptions>? configure = null)
        where TContext : SummaryDbContext
        => services.AddMetricsPipeline<TContext>(dbCfg, o => { o.WorkerType = workerType; configure?.Invoke(o); });

    /// <summary>
    /// Registers the pipeline using a custom context type.
    /// </summary>
    /// <typeparam name="TContext">The context implementing <see cref="SummaryDbContext"/>.</typeparam>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="dbCfg">Action configuring the database context.</param>
    /// <param name="configure">Optional action to configure additional services.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddMetricsPipeline<TContext>(this IServiceCollection services, Action<DbContextOptionsBuilder> dbCfg, Action<MetricsPipelineOptions>? configure = null)
        where TContext : SummaryDbContext
    {
        var options = new MetricsPipelineOptions();
        configure?.Invoke(options);

        services.AddDbContext<SummaryDbContext, TContext>(dbCfg);

        if (options.WorkerMode == WorkerMode.Http)
        {
            RegisterHttpClient(services, options);
            services.AddTransient<IWorkerService, HttpWorkerService>();
            services.AddTransient<IGatherService, ListGatherService>();
        }
        else
        {
            services.AddScoped<ListGatherService>();
            services.AddScoped<IGatherService>(sp => sp.GetRequiredService<ListGatherService>());
            services.AddScoped<IWorkerService>(sp => sp.GetRequiredService<ListGatherService>());
            if (options.RegisterHttpClient)
            {
                RegisterHttpClient(services, options);
            }
        }

        services.AddTransient<ISummarizationService, InMemorySummarizationService>();
        services.AddTransient<IValidationService, ThresholdValidationService>();
        services.AddTransient<ICommitService, EfCommitService>();
        services.AddTransient<IDiscardHandler, LoggingDiscardHandler>();
        services.AddTransient<ISummaryRepository, EfSummaryRepository>();
        services.AddTransient<IPipelineOrchestrator, PipelineOrchestrator>();

        if (options.AddWorker)
        {
            var workerType = options.WorkerType ?? typeof(PipelineWorker);
            services.AddSingleton(typeof(IHostedService), workerType);
        }

        return services;
    }

    private static void RegisterHttpClient(IServiceCollection services, MetricsPipelineOptions options)
    {
        if (options.ConfigureClient != null)
            services.AddHttpClient<HttpMetricsClient>(options.ConfigureClient);
        else
            services.AddHttpClient<HttpMetricsClient>();
    }
}
