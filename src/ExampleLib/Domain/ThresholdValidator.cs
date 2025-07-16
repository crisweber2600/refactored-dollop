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
    /// <param name="throwOnUnsupported">Throw when the <see cref="ThresholdType"/> is not recognised.</param>
    public static bool IsWithinThreshold(
        decimal current,
        decimal previous,
        ThresholdType type,
        decimal threshold,
        bool throwOnUnsupported = false)
    {
        switch (type)
        {
            case ThresholdType.RawDifference:
                return Math.Abs(current - previous) <= threshold;
            case ThresholdType.PercentChange:
                if (previous == 0)
                {
                    return current == 0;
                }
                var changeFraction = Math.Abs((current - previous) / previous);
                return changeFraction <= threshold;
            default:
                if (throwOnUnsupported)
                    throw new NotSupportedException($"Unsupported ThresholdType: {type}");
                return true;
        }
    }
}
