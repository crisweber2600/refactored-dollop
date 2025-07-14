using System;
using System.Collections.Generic;

namespace ExampleLib.Domain;

/// <summary>
/// Provides helpers for validating ordered sequences of items based on dynamic selectors.
/// </summary>
public static class SequenceValidator
{
    /// <summary>
    /// Validates a sequence by comparing the selected value of each item to the most recent prior item with a different key.
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
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        if (wheneverSelector == null) throw new ArgumentNullException(nameof(wheneverSelector));
        if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));
        if (validationFunc == null) throw new ArgumentNullException(nameof(validationFunc));

        using var enumerator = items.GetEnumerator();
        if (!enumerator.MoveNext())
            return true;

        var lastItem = enumerator.Current;
        var lastValue = valueSelector(lastItem);

        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            var currentValue = valueSelector(current);

            if (!validationFunc(currentValue, lastValue))
                return false;

            lastItem = current;
            lastValue = currentValue;
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
    {
        if (plan == null) throw new ArgumentNullException(nameof(plan));

        return Validate(items, wheneverSelector, plan.MetricSelector, (cur, prev) =>
        {
            switch (plan.ThresholdType)
            {
                case ThresholdType.RawDifference:
                    return Math.Abs(cur - prev) <= plan.ThresholdValue;
                case ThresholdType.PercentChange:
                    if (prev == 0) return cur == 0;
                    var change = Math.Abs((cur - prev) / prev);
                    return change <= plan.ThresholdValue;
                default:
                    throw new NotSupportedException($"Unsupported ThresholdType: {plan.ThresholdType}");
            }
        });
    }
}
