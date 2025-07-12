using System.Collections.Concurrent;
using ExampleLib;

namespace ExampleData.Infrastructure;

public class DataInMemoryValidationPlanProvider : IValidationPlanProvider
{
    private readonly ConcurrentDictionary<Type, object> _plans = new();

    public void AddPlan<T>(ValidationPlan<T> plan)
    {
        _plans[typeof(T)] = plan;
    }

    public ValidationPlan<T> GetPlan<T>()
    {
        if (_plans.TryGetValue(typeof(T), out var obj) && obj is ValidationPlan<T> plan)
            return plan;
        throw new KeyNotFoundException($"No ValidationPlan registered for type {typeof(T).Name}");
    }
}
