namespace ExampleLib.Domain;

/// <summary>
/// Store for summarisation plans, providing the plan for a given entity type.
/// </summary>
public interface ISummarisationPlanStore
{
    /// <summary>Retrieve the SummarisationPlan for entity type T.</summary>
    SummarisationPlan<T> GetPlan<T>();
}
