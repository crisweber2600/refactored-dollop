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
}
