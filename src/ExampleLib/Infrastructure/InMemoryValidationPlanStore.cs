using ExampleLib.Domain;

namespace ExampleLib.Infrastructure;

/// <summary>
/// In-memory implementation of IValidationPlanStore for storing validation plans.
/// </summary>
public class InMemoryValidationPlanStore : IValidationPlanStore
{
    private readonly Dictionary<Type, ValidationPlan> _plans = new();

    /// <inheritdoc />
    public ValidationPlan? GetPlan<T>()
    {
        return _plans.TryGetValue(typeof(T), out var plan) ? plan : null;
    }

    /// <inheritdoc />
    public bool HasPlan<T>()
    {
        return _plans.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Add a validation plan for the specified entity type.
    /// </summary>
    /// <param name="plan">The validation plan</param>
    public void AddPlan(ValidationPlan plan)
    {
        if (plan == null) throw new ArgumentNullException(nameof(plan));
        _plans[plan.EntityType] = plan;
    }

    /// <summary>
    /// Remove a validation plan for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public void RemovePlan<T>()
    {
        _plans.Remove(typeof(T));
    }

    /// <summary>
    /// Clear all validation plans.
    /// </summary>
    public void Clear()
    {
        _plans.Clear();
    }
}