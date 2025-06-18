using System.Collections.Concurrent;
using ExampleLib.Domain;

namespace ExampleLib.Infrastructure;

/// <summary>
/// In-memory implementation of <see cref="ISummarisationPlanStore"/> for testing.
/// </summary>
public class InMemorySummarisationPlanStore : ISummarisationPlanStore
{
    private readonly ConcurrentDictionary<Type, object> _plans = new();

    /// <summary>
    /// Register a <see cref="SummarisationPlan{T}"/> for a specific entity type.
    /// </summary>
    public void AddPlan<T>(SummarisationPlan<T> plan)
    {
        _plans[typeof(T)] = plan;
    }

    /// <summary>
    /// Retrieve the plan for the specified entity type.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when no plan is registered for <typeparamref name="T"/>.</exception>
    public SummarisationPlan<T> GetPlan<T>()
    {
        if (_plans.TryGetValue(typeof(T), out var obj) && obj is SummarisationPlan<T> plan)
        {
            return plan;
        }
        throw new KeyNotFoundException($"No SummarisationPlan registered for type {typeof(T).Name}");
    }
}
