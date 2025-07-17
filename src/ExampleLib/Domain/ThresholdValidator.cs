using System;

namespace ExampleLib.Domain;

/// <summary>
/// Provides helper methods for validating metric changes against thresholds.
/// </summary>
public static class ThresholdValidator
{
    /// <summary>
    /// Determines if the difference between the current and previous value
    /// falls within the allowed threshold.
    /// </summary>
    /// <param name="current">Current metric value.</param>
    /// <param name="previous">Previous metric value.</param>
    /// <param name="type">Type of comparison to perform.</param>
    /// <param name="threshold">Allowed threshold value.</param>
    /// <param name="validated">Whether the change has already been validated.</param>
    /// <param name="throwOnUnsupported">Throw when the <see cref="ThresholdType"/> is not recognised.</param>
    public static bool IsWithinThreshold(
        decimal current,
        decimal previous,
        ThresholdType type,
        decimal threshold,
        bool validated = false,
        bool throwOnUnsupported = false)
    {
        if (threshold < 0)
            throw new ArgumentException("Threshold value cannot be negative.", nameof(threshold));

        // If already validated, always allow
        if (validated)
            return true;

        switch (type)
        {
            case ThresholdType.RawDifference:
                return Math.Abs(current - previous) <= threshold;
            case ThresholdType.PercentChange:
                return ValidatePercentageChange(current, previous, threshold);
            default:
                if (throwOnUnsupported)
                    throw new NotSupportedException($"Unsupported ThresholdType: {type}");
                return true;
        }
    }

    private static bool ValidatePercentageChange(decimal current, decimal previous, decimal threshold)
    {
        if (previous == 0)
        {
            // If previous is zero, only allow if current is also zero
            return current == 0;
        }
        
        // Calculate percentage change as a decimal fraction (e.g., 0.2 = 20%)
        var percentageChange = Math.Abs((current - previous) / previous);
        
        // The threshold is expected to be in decimal format (e.g., 0.1 = 10%)
        return percentageChange <= threshold;
    }
}
