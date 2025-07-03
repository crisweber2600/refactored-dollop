using System;
using System.Collections.Generic;

namespace ExampleLib.Domain;

/// <summary>
/// Simple implementation of <see cref="IManualValidatorService"/>
/// using a dictionary of validation rules keyed by type.
/// </summary>
public class ManualValidatorService : IManualValidatorService
{
    private readonly IDictionary<Type, List<Func<object, bool>>> _rules;

    public ManualValidatorService(IDictionary<Type, List<Func<object, bool>>> rules)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
    }

    /// <inheritdoc />
    public bool Validate(object instance)
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        var type = instance.GetType();
        if (!_rules.TryGetValue(type, out var rules) || rules.Count == 0)
        {
            return true;
        }

        foreach (var rule in rules)
        {
            if (!rule(instance))
            {
                return false;
            }
        }
        return true;
    }
}
