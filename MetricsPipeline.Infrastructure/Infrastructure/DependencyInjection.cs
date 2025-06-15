namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

public static class DependencyInjection
{
    public static IServiceCollection AddMetricsPipeline(this IServiceCollection services, Action<DbContextOptionsBuilder> dbCfg)
    {
        services.AddDbContext<SummaryDbContext>(dbCfg);
        // Use a scoped lifetime for the in-memory gather service so that
        // the orchestrator and step definitions share the same instance
        // within a single scenario while avoiding cross-scenario state.
        services.AddScoped<IGatherService, InMemoryGatherService>();
        services.AddTransient<ISummarizationService, InMemorySummarizationService>();
        services.AddTransient<IValidationService, ThresholdValidationService>();
        services.AddTransient<ICommitService, EfCommitService>();
        services.AddTransient<IDiscardHandler, LoggingDiscardHandler>();
        services.AddTransient<ISummaryRepository, EfSummaryRepository>();
        services.AddTransient<IPipelineOrchestrator, PipelineOrchestrator>();
        return services;
    }
}
