using MetricsPipeline.Infrastructure;
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
        services.AddMetricsPipeline(o => o.UseInMemoryDatabase("summaries"));
        return services;
    }
}
