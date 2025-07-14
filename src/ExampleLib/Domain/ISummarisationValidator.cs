
namespace ExampleLib.Domain;

/// <summary>
/// Validates an entity's save against summarisation rules.
/// </summary>
public interface ISummarisationValidator<T>
{
    /// <summary>
    /// Validates the current entity against the previous audit record using the provided summarisation plan.
    /// Returns true if validation passes (within thresholds), false if the change is out of bounds.
    /// </summary>
    bool Validate(T currentEntity, SaveAudit? previousAudit, SummarisationPlan<T> plan);
}
