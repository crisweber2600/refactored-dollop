namespace ExampleLib.Domain;

/// <summary>
/// Defines how to compute a summarisation metric from an entity of type T and how to validate changes.
/// </summary>
public class SummarisationPlan<T>
{
    /// <summary>Function to select and aggregate a numeric metric from the entity (e.g., Sum of a collection).</summary>
    public Func<T, decimal> MetricSelector { get; }

    /// <summary>The type of threshold logic to apply for change detection.</summary>
    public ThresholdType ThresholdType { get; }

    /// <summary>The allowed threshold (raw difference amount or percentage as decimal fraction) for change.</summary>
    public decimal ThresholdValue { get; }

    public SummarisationPlan(Func<T, decimal> metricSelector, ThresholdType thresholdType, decimal thresholdValue)
    {
        MetricSelector = metricSelector ?? throw new ArgumentNullException(nameof(metricSelector));
        ThresholdType = thresholdType;
        ThresholdValue = thresholdValue;
    }

    /// <summary>
    /// Validates a sequence by comparing the selected value of each item to the most
    /// recent prior item with the **same** discriminator key.
    /// </summary>
    public static bool Validate<TItem, TKey, TValue>(
        IEnumerable<TItem> items,
        Func<TItem, TKey> wheneverSelector,
        Func<TItem, TValue> valueSelector,
        Func<TValue, TValue, bool> validationFunc)
        where TKey : notnull
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        if (wheneverSelector == null) throw new ArgumentNullException(nameof(wheneverSelector));
        if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));
        if (validationFunc == null) throw new ArgumentNullException(nameof(validationFunc));

        var lastValues = new Dictionary<TKey, TValue>();

        foreach (var item in items)
        {
            var key = wheneverSelector(item);
            var value = valueSelector(item);

            if (lastValues.TryGetValue(key, out var previous))
            {
                if (!validationFunc(value, previous))
                    return false;
            }

            lastValues[key] = value;
        }

        return true;
    }

    /// <summary>
    /// Validates a sequence using default equality comparison on the selected value.
    /// </summary>
    public static bool Validate<TItem, TKey, TValue>(
        IEnumerable<TItem> items,
        Func<TItem, TKey> wheneverSelector,
        Func<TItem, TValue> valueSelector)
        where TKey : notnull
    {
        return Validate(items, wheneverSelector, valueSelector, (c, p) => EqualityComparer<TValue>.Default.Equals(c, p));
    }

    /// <summary>
    /// Validates a sequence using a <see cref="SummarisationPlan{T}"/>. Metric values
    /// are compared according to the plan's threshold rules.
    /// </summary>
    public static bool Validate<TItem, TKey>(
        IEnumerable<TItem> items,
        Func<TItem, TKey> wheneverSelector,
        SummarisationPlan<TItem> plan)
        where TKey : notnull
    {
        if (plan == null) throw new ArgumentNullException(nameof(plan));

        return Validate(items, wheneverSelector, plan.MetricSelector, (cur, prev) =>
            ThresholdValidator.IsWithinThreshold(
                cur,
                prev,
                plan.ThresholdType,
                plan.ThresholdValue,
                throwOnUnsupported: true));
    }
}
