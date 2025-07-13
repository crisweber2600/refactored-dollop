using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using ExampleData.Infrastructure;
using System;
using System.Linq;

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
    /// Adds MongoDB services using the provided connection string and database name.
    /// Registers the Mongo client, database, validation service and unit of work.
    /// </summary>
    public static IServiceCollection AddExampleDataMongo(
        this IServiceCollection services,
        string connectionString,
        string databaseName)
    {
        services.AddSingleton(new MongoClient(connectionString));
        services.AddSingleton<IMongoDatabase>(sp =>
            sp.GetRequiredService<MongoClient>().GetDatabase(databaseName));
        services.AddScoped<IValidationService, MongoValidationService>();
        services.AddScoped<IUnitOfWork, MongoUnitOfWork>();
        services.AddScoped(typeof(IMongoCollectionInterceptor<>), typeof(MongoCollectionInterceptor<>));
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
        services.AddSingleton<ExampleLib.IValidationPlanProvider, ExampleData.Infrastructure.DataInMemoryValidationPlanProvider>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(EfGenericRepository<>));
        return services;
    }

    /// <summary>
    /// Generic MongoDB setup mirroring <see cref="SetupDatabase{TContext}"/>.
    /// Registers the Mongo client, database, validation service and unit of work.
    /// </summary>
    public static IServiceCollection SetupMongoDatabase(
        this IServiceCollection services,
        string connectionString,
        string databaseName)
    {
        services.AddSingleton(new MongoClient(connectionString));
        services.AddSingleton<IMongoDatabase>(sp =>
            sp.GetRequiredService<MongoClient>().GetDatabase(databaseName));
        services.AddScoped<IValidationService, MongoValidationService>();
        services.AddScoped<IUnitOfWork, MongoUnitOfWork>();
        services.AddScoped(typeof(IMongoCollectionInterceptor<>), typeof(MongoCollectionInterceptor<>));
        services.AddSingleton<ExampleLib.IValidationPlanProvider, ExampleData.Infrastructure.DataInMemoryValidationPlanProvider>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(MongoGenericRepository<>));
        return services;
    }

    /// <summary>
    /// Decorate <see cref="IMongoDbService"/> so inserts automatically
    /// trigger validation using registered plans.
    /// </summary>
    public static IServiceCollection AddMongoDbServiceValidation(this IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMongoDbService));
        if (descriptor == null)
            throw new InvalidOperationException("IMongoDbService not registered");

        var lifetime = descriptor.Lifetime;
        services.Remove(descriptor);
        services.Add(new ServiceDescriptor(typeof(IMongoDbService), sp =>
        {
            IMongoDbService inner = descriptor.ImplementationInstance as IMongoDbService
                ?? (descriptor.ImplementationFactory != null
                    ? (IMongoDbService)descriptor.ImplementationFactory(sp)
                    : (IMongoDbService)ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType!));
            var uow = sp.GetRequiredService<IUnitOfWork>();
            return new MongoDbServiceValidationDecorator(inner, uow);
        }, lifetime));
        return services;
    }
}
