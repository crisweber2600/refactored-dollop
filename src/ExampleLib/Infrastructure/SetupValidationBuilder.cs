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
    private bool _useMongo;

    /// <summary>
    /// Indicates whether MongoDB has been configured.
    /// </summary>
    internal bool UsesMongo => _useMongo;

    /// <summary>
    /// Record SQL Server configuration using the specified DbContext.
    /// </summary>
    public SetupValidationBuilder UseSqlServer<TContext>(string connectionString)
        where TContext : YourDbContext
    {
        _steps.Add(s => s.SetupDatabase<TContext>(connectionString));
        _useMongo = false;
        return this;
    }

    /// <summary>
    /// Record Mongo configuration with the provided connection string and database name.
    /// </summary>
    public SetupValidationBuilder UseMongo(string connectionString, string databaseName)
    {
        _steps.Add(s => s.SetupMongoDatabase(connectionString, databaseName));
        _useMongo = true;
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
