using System.Collections.Concurrent;
using ExampleLib;

namespace ExampleLib.Infrastructure;

/// <summary>
/// In-memory implementation of <see cref="IValidationPlanProvider"/> for testing.
/// </summary>
public class InMemoryValidationPlanProvider : IValidationPlanProvider
{
    private readonly ConcurrentDictionary<Type, object> _plans = new();

    /// <summary>
    /// Register a <see cref="ValidationPlan{T}"/> for a specific entity type.
    /// </summary>
    public void AddPlan<T>(ValidationPlan<T> plan)
    {
        _plans[typeof(T)] = plan;
    }

    /// <summary>
    /// Retrieve the plan for the specified entity type.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when no plan is registered for <typeparamref name="T"/>.</exception>
    public ValidationPlan<T> GetPlan<T>()
    {
        if (_plans.TryGetValue(typeof(T), out var obj) && obj is ValidationPlan<T> plan)
        {
            return plan;
        }
        throw new KeyNotFoundException($"No ValidationPlan registered for type {typeof(T).Name}");
    }
}
