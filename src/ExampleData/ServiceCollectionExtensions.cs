using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleData;

/// <summary>
/// Extension methods for registering ExampleData services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="YourDbContext"/> configured for SQL Server and registers
    /// supporting services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The connection string to the SQL Server database.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddExampleDataSqlServer(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<YourDbContext>(o => o.UseSqlServer(connectionString));
        services.AddScoped<IValidationService, ValidationService>();
        services.AddScoped<IUnitOfWork, UnitOfWork<YourDbContext>>();
        return services;
    }

    /// <summary>
    /// Generic database setup used by production examples.
    /// Registers the DbContext, validation service and unit of work.
    /// </summary>
    public static IServiceCollection SetupDatabase<TContext>(
        this IServiceCollection services,
        string connectionString)
        where TContext : YourDbContext
    {
        services.AddDbContext<TContext>(o => o.UseSqlServer(connectionString));
        services.AddScoped<IValidationService, ValidationService>();
        services.AddScoped<IUnitOfWork, UnitOfWork<TContext>>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(EfGenericRepository<>));
        return services;
    }
}
