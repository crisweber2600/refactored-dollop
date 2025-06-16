namespace MetricsPipeline.Infrastructure;
using MassTransit;
using MetricsPipeline.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositoriesAndSagas<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> dbCfg,
        Action<IBusRegistrationConfigurator>? busCfg = null)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>(dbCfg);
        services.AddScoped<DbContext>(p => p.GetRequiredService<TContext>());
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IGenericRepository<>), typeof(EfGenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork<TContext>>();
        services.AddMassTransit(cfg => busCfg?.Invoke(cfg));
        return services;
    }
}
