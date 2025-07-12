using System;
using ExampleLib;

namespace ExampleLib.Domain;

/// <summary>
/// Default implementation of <see cref="ISummarisationValidator{T}"/>.
/// Compares the current entity metric with the previous saved metric
/// according to the rules defined in a <see cref="ValidationPlan{T}"/>.
/// </summary>
public class SummarisationValidator<T> : ISummarisationValidator<T>
{
    /// <inheritdoc />
    public bool Validate(T currentEntity, SaveAudit previousAudit, ValidationPlan<T> plan)
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

        switch (plan.ThresholdType)
        {
            case ThresholdType.RawDifference:
                var diff = currentValue - previousValue;
                return Math.Abs(diff) <= plan.ThresholdValue;

            case ThresholdType.PercentChange:
                if (previousValue == 0)
                {
                    // If previous value was zero, treat any non-zero change as exceeding
                    return currentValue == 0;
                }

                var changeFraction = Math.Abs((currentValue - previousValue) / previousValue);
                return changeFraction <= plan.ThresholdValue;

            default:
                // Unknown threshold type - treat as valid
                return true;
        }
    }
}
