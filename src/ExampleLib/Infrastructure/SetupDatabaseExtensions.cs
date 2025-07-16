using ExampleLib.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace ExampleLib.Infrastructure;

public static class SetupDatabaseExtensions
{
    public static IServiceCollection SetupDatabase<TContext>(
        this IServiceCollection services,
        string connectionString)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>(o => o.UseSqlServer(connectionString));
        services.AddScoped<IValidationService, ValidationService>();
        services.AddScoped<IUnitOfWork, UnitOfWork<TContext>>();
        services.AddSingleton<ISummarisationPlanStore, InMemorySummarisationPlanStore>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(EfGenericRepository<>));
        return services;
    }

    public static IServiceCollection SetupMongoDatabase(
        this IServiceCollection services,
        string connectionString,
        string databaseName)
    {
        services.AddSingleton(new MongoClient(connectionString));
        services.AddSingleton<IMongoDatabase>(sp =>
            sp.GetRequiredService<MongoClient>().GetDatabase(databaseName));
        services.AddScoped<IValidationService, ValidationService>();
        services.AddScoped<IUnitOfWork, MongoUnitOfWork>();
        services.AddScoped(typeof(IMongoCollectionInterceptor<>), typeof(MongoCollectionInterceptor<>));
        services.AddSingleton<ISummarisationPlanStore, InMemorySummarisationPlanStore>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(MongoGenericRepository<>));
        return services;
    }
}
