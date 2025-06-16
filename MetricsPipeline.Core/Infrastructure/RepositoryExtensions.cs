namespace MetricsPipeline.Infrastructure;
using MassTransit;
using MetricsPipeline.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering repositories and MassTransit sagas.
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Adds repository implementations and MassTransit configuration.
    /// </summary>
    /// <typeparam name="TContext">Database context type.</typeparam>
    /// <param name="services">Service collection to modify.</param>
    /// <param name="dbCfg">Action configuring the context.</param>
    /// <param name="busCfg">Optional MassTransit configuration.</param>
    /// <returns>The updated service collection.</returns>
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
