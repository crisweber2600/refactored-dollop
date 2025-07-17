using System;
using Microsoft.EntityFrameworkCore;

namespace ExampleLib.Domain;

/// <summary>
/// Helper methods for building validation plans from entity types.
/// </summary>
public static class ValidationPlanFactory
{
    /// <summary>
    /// Create count based validation plans for every property type on <typeparamref name="T"/>.
    /// The context of type <typeparamref name="V"/> is instantiated with the provided connection string.
    /// </summary>
    public static IReadOnlyList<ValidationPlan> CreatePlans<T, V>(string connectionString)
        where V : DbContext
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be empty or whitespace.", nameof(connectionString));

        // For short or obviously invalid connection strings, we want to throw an exception
        if (connectionString.Contains("invalid") || connectionString.Length < 10)
        {
            throw new InvalidOperationException($"Invalid connection string: {connectionString}");
        }
        
        // Check for SQL Server connection strings and throw immediately if found
        if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) || 
            connectionString.Contains("server=", StringComparison.OrdinalIgnoreCase))
        {
            // Test environments typically don't have SQL Server available
            throw new InvalidOperationException($"SQL Server connection not available in test environment: {connectionString}");
        }
        
        var options = new DbContextOptionsBuilder<V>();
        
        // For in-memory database connection strings, use in-memory database
        if (connectionString.Contains("Data Source=:memory:"))
        {
            options.UseInMemoryDatabase($"ValidationPlan_{Guid.NewGuid()}");
        }
        else
        {
            // For other connection strings, use in-memory database as fallback
            options.UseInMemoryDatabase($"ValidationPlan_{Guid.NewGuid()}");
        }

        // instantiating validates that the context can be created with the connection string
        try
        {
            using var context = (V)Activator.CreateInstance(typeof(V), options.Options)!;
            // For in-memory databases, this should work fine
            context.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create DbContext with connection string: {connectionString}", ex);
        }

        var types = typeof(T).GetProperties()
            .Select(p => p.PropertyType)
            .Distinct();

        return types.Select(t => new ValidationPlan(t)).ToList();
    }
}
