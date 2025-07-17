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
    /// Retrieve the plan for the specified entity type. Returns null if no plan exists.
    /// </summary>
    public SummarisationPlan<T>? GetPlan<T>()
    {
        if (_plans.TryGetValue(typeof(T), out var obj) && obj is SummarisationPlan<T> plan)
        {
            return plan;
        }
        return null;
    }

    /// <summary>
    /// Check if a SummarisationPlan exists for entity type T.
    /// </summary>
    public bool HasPlan<T>()
    {
        return _plans.ContainsKey(typeof(T));
    }
}
