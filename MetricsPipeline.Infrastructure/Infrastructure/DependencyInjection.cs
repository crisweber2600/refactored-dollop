namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

public static class DependencyInjection
{
    public static IServiceCollection AddMetricsPipeline(this IServiceCollection services, Action<DbContextOptionsBuilder> dbCfg)
    {
        services.AddDbContext<SummaryDbContext>(dbCfg);
        services.AddTransient<IGatherService, InMemoryGatherService>();
        services.AddTransient<ISummarizationService, InMemorySummarizationService>();
        services.AddTransient<IValidationService, ThresholdValidationService>();
        services.AddTransient<ICommitService, EfCommitService>();
        services.AddTransient<IDiscardHandler, LoggingDiscardHandler>();
        services.AddTransient<ISummaryRepository, EfSummaryRepository>();
        services.AddTransient<IPipelineOrchestrator, PipelineOrchestrator>();
        return services;
    }
}
