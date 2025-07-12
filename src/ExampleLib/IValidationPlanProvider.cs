namespace ExampleLib;

/// <summary>
/// Store for summarisation plans, providing the plan for a given entity type.
/// </summary>
public interface IValidationPlanProvider
{
    /// <summary>Retrieve the <see cref="ValidationPlan{T}"/> for entity type T.</summary>
    ValidationPlan<T> GetPlan<T>();
}
