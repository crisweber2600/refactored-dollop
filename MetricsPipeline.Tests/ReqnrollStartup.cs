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
        var provider = Environment.GetEnvironmentVariable("DB_PROVIDER");

        void ConfigureDb(DbContextOptionsBuilder o)
        {
            if (string.Equals(provider, "sqlite", StringComparison.OrdinalIgnoreCase))
                o.UseSqlite($"Data Source={Guid.NewGuid()};Mode=Memory;Cache=Shared");
            else
                o.UseInMemoryDatabase(Guid.NewGuid().ToString());
        }

        services.AddMetricsPipeline(ConfigureDb);
        services.AddHttpClient<HttpMetricsClient>();
        services.AddRepositoriesAndSagas<SummaryDbContext>(
            ConfigureDb,
            cfg => cfg.UsingInMemory((context, c) => { }));
        return services;
    }
}
