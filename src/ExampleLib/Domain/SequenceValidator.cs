using System;
using System.Collections.Generic;

namespace ExampleLib.Domain;

/// <summary>
/// Provides helpers for validating ordered sequences of items based on dynamic selectors.
/// </summary>
public static class SequenceValidator
{
    /// <summary>
    /// Validates a sequence by comparing the selected value of each item to the most
    /// recent prior item with the **same** discriminator key.
    /// </summary>
    /// <param name="items">Items to validate in order.</param>
    /// <param name="wheneverSelector">Selects a discriminator key.</param>
    /// <param name="valueSelector">Selects the value used for comparison.</param>
    /// <param name="validationFunc">Determines if the current value is valid compared to the previous.</param>
    public static bool Validate<T, TKey, TValue>(
        IEnumerable<T> items,
        Func<T, TKey> wheneverSelector,
        Func<T, TValue> valueSelector,
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
    public static bool Validate<T, TKey, TValue>(
        IEnumerable<T> items,
        Func<T, TKey> wheneverSelector,
        Func<T, TValue> valueSelector)
        where TKey : notnull
    {
        return Validate(items, wheneverSelector, valueSelector, (c, p) => EqualityComparer<TValue>.Default.Equals(c, p));
    }

    /// <summary>
    /// Validates a sequence using a <see cref="SummarisationPlan{T}"/>. Metric values
    /// are compared according to the plan's threshold rules.
    /// </summary>
    public static bool Validate<T, TKey>(
        IEnumerable<T> items,
        Func<T, TKey> wheneverSelector,
        SummarisationPlan<T> plan)
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
