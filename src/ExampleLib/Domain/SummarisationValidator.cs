namespace ExampleLib.Domain;

/// <summary>
/// Default implementation of <see cref="ISummarisationValidator{T}"/>.
/// Compares the current entity metric with the previous saved metric
/// according to the rules defined in a <see cref="SummarisationPlan{T}"/>.
/// </summary>
public class SummarisationValidator<T> : ISummarisationValidator<T>
{
    /// <inheritdoc />
    public bool Validate(T currentEntity, SaveAudit? previousAudit, SummarisationPlan<T> plan)
    {
        if (plan == null) throw new ArgumentNullException(nameof(plan));
        if (currentEntity == null) throw new ArgumentNullException(nameof(currentEntity));

        // Compute current metric value from the entity
        decimal currentValue = plan.MetricSelector(currentEntity);

        // No previous audit means there is no baseline to compare against
        if (previousAudit == null)
        {
            return true;
        }

        decimal previousValue = previousAudit.MetricValue;

        return ThresholdValidator.IsWithinThreshold(
            currentValue,
            previousValue,
            plan.ThresholdType,
            plan.ThresholdValue);
    }
}
