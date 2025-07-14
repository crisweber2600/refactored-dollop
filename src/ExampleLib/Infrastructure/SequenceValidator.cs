using System.Collections.Generic;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Validates a sequence of items by comparing the selected value
/// whenever the discriminator property changes.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <typeparam name="TKey">Discriminator key type.</typeparam>
public class SequenceValidator<T, TKey>
{
    private readonly Func<T, TKey> _wheneverSelector;
    private readonly Func<T, decimal> _valueSelector;
    private readonly Func<decimal, decimal, bool> _rule;
    private readonly List<T> _history = new();

    public SequenceValidator(
        Func<T, TKey> wheneverSelector,
        Func<T, decimal> valueSelector,
        Func<decimal, decimal, bool> rule)
    {
        _wheneverSelector = wheneverSelector ?? throw new ArgumentNullException(nameof(wheneverSelector));
        _valueSelector = valueSelector ?? throw new ArgumentNullException(nameof(valueSelector));
        _rule = rule ?? throw new ArgumentNullException(nameof(rule));
    }

    /// <summary>
    /// Validate the provided instance against the most recent prior instance
    /// where the discriminator value differs.
    /// </summary>
    public bool Validate(T instance)
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        var key = _wheneverSelector(instance);
        var value = _valueSelector(instance);

        for (int i = _history.Count - 1; i >= 0; i--)
        {
            var previous = _history[i];
            if (!EqualityComparer<TKey>.Default.Equals(_wheneverSelector(previous), key))
            {
                var priorValue = _valueSelector(previous);
                var valid = _rule(value, priorValue);
                _history.Add(instance);
                return valid;
            }
        }

        _history.Add(instance);
        return true;
    }

    /// <summary>
    /// Clears the stored history for reuse.
    /// </summary>
    public void Reset() => _history.Clear();
}
