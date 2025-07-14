using System;
using System.Collections.Generic;
using System.Linq;

namespace ExampleLib.Domain;

/// <summary>
/// Provides helpers for validating sequences using dynamic property selectors.
/// </summary>
public static class SequenceValidator
{
    /// <summary>
    /// Validates a sequence of items where comparisons occur whenever the
    /// discriminator selector value changes.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <param name="source">Items in chronological order.</param>
    /// <param name="wheneverSelector">Selector producing the discriminator key.</param>
    /// <param name="valueSelector">Selector producing the numeric value.</param>
    /// <param name="rule">Validation rule applied to the current and prior values.</param>
    /// <returns><c>true</c> when all comparisons pass or no prior item exists.</returns>
    public static bool Validate<T, TKey>(
        IEnumerable<T> source,
        Func<T, TKey> wheneverSelector,
        Func<T, decimal> valueSelector,
        Func<decimal, decimal, bool> rule)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (wheneverSelector == null) throw new ArgumentNullException(nameof(wheneverSelector));
        if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));
        if (rule == null) throw new ArgumentNullException(nameof(rule));

        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            return true; // empty sequence

        var previous = enumerator.Current;
        var previousKey = wheneverSelector(previous);

        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            var currentKey = wheneverSelector(current);
            if (!EqualityComparer<TKey>.Default.Equals(currentKey, previousKey))
            {
                var currentValue = valueSelector(current);
                var previousValue = valueSelector(previous);
                if (!rule(currentValue, previousValue))
                    return false;
            }
            previous = current;
            previousKey = currentKey;
        }

        return true;
    }
}
