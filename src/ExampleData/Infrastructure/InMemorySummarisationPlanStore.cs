using System.Collections.Concurrent;
using ExampleLib.Domain;

namespace ExampleData.Infrastructure;

public class DataInMemorySummarisationPlanStore : ISummarisationPlanStore
{
    private readonly ConcurrentDictionary<Type, object> _plans = new();

    public void AddPlan<T>(SummarisationPlan<T> plan)
    {
        _plans[typeof(T)] = plan;
    }

    public SummarisationPlan<T> GetPlan<T>()
    {
        if (_plans.TryGetValue(typeof(T), out var obj) && obj is SummarisationPlan<T> plan)
            return plan;
        throw new KeyNotFoundException($"No SummarisationPlan registered for type {typeof(T).Name}");
    }
}
