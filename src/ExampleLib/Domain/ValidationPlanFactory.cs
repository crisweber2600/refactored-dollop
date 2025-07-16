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
        var options = new DbContextOptionsBuilder<V>()
            .UseSqlServer(connectionString)
            .Options;
        // instantiating validates that the context can be created with the connection string
        using var _ = (V)Activator.CreateInstance(typeof(V), options)!;

        var types = typeof(T).GetProperties()
            .Select(p => p.PropertyType)
            .Distinct();

        return types.Select(t => new ValidationPlan(t)).ToList();
    }
}
