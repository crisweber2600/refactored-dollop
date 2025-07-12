namespace ExampleLib;

/// <summary>
/// Provides <see cref="ValidationPlan{T}"/> instances for entity types.
/// </summary>
public interface IValidationPlanProvider
{
    /// <summary>Add a validation plan for entity type <typeparamref name="T"/>.</summary>
    void AddPlan<T>(ValidationPlan<T> plan);

    /// <summary>Retrieve the plan for entity type <typeparamref name="T"/>.</summary>
    ValidationPlan<T> GetPlan<T>();
}
