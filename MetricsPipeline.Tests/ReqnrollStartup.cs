using MetricsPipeline.Infrastructure;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Reqnroll;
using Reqnroll.Microsoft.Extensions.DependencyInjection;

public class ReqnrollStartup
{
    [ScenarioDependencies]
    public static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        // Use a unique in-memory database for each scenario to avoid
        // cross-test interference when persisting summaries.
        services.AddMetricsPipeline(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddRepositoriesAndSagas<SummaryDbContext>(
            o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()),
            cfg => cfg.UsingInMemory((context, c) => { }));
        return services;
    }
}
