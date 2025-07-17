namespace ExampleLib.Domain;

/// <summary>
/// Store for summarisation plans, providing the plan for a given entity type.
/// </summary>
public interface ISummarisationPlanStore
{
    /// <summary>Retrieve the SummarisationPlan for entity type T. Returns null if no plan exists.</summary>
    SummarisationPlan<T>? GetPlan<T>();
    
    /// <summary>Check if a SummarisationPlan exists for entity type T.</summary>
    bool HasPlan<T>();
}
