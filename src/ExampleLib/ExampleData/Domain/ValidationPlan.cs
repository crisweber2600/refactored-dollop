namespace ExampleLib;

using ExampleLib.Domain;

/// <summary>
/// Defines how to compute a summarisation metric from an entity of type T and how to validate changes.
/// </summary>
public class ValidationPlan<T>
{
    /// <summary>Function to select and aggregate a numeric metric from the entity (e.g., Sum of a collection).</summary>
    public Func<T, decimal> MetricSelector { get; }

    /// <summary>The type of threshold logic to apply for change detection.</summary>
    public ThresholdType ThresholdType { get; }

    /// <summary>The allowed threshold (raw difference amount or percentage as decimal fraction) for change.</summary>
    public decimal ThresholdValue { get; }

    public ValidationPlan(Func<T, decimal> metricSelector, ThresholdType thresholdType, decimal thresholdValue)
    {
        MetricSelector = metricSelector;
        ThresholdType = thresholdType;
        ThresholdValue = thresholdValue;
    }
}
