using System;
using System.Collections.Generic;
using ExampleData;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Fluent builder capturing service configuration steps.
/// </summary>
public class SetupValidationBuilder
{
    private readonly List<Action<IServiceCollection>> _steps = new();

    /// <summary>
    /// Record SQL Server configuration using the specified DbContext.
    /// </summary>
    public SetupValidationBuilder UseSqlServer<TContext>(string connectionString)
        where TContext : YourDbContext
    {
        _steps.Add(s => s.SetupDatabase<TContext>(connectionString));
        return this;
    }

    /// <summary>
    /// Record Mongo configuration with the provided connection string and database name.
    /// </summary>
    public SetupValidationBuilder UseMongo(string connectionString, string databaseName)
    {
        _steps.Add(s => s.SetupMongoDatabase(connectionString, databaseName));
        return this;
    }

    /// <summary>
    /// Apply all recorded steps to the service collection.
    /// </summary>
    public IServiceCollection Apply(IServiceCollection services)
    {
        foreach (var step in _steps)
        {
            step(services);
        }
        return services;
    }
}
