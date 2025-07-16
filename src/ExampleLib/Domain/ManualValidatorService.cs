namespace ExampleLib.Domain;

/// <summary>
/// Simple implementation of <see cref="IManualValidatorService"/>
/// using a dictionary of validation rules keyed by type.
/// </summary>
public class ManualValidatorService : IManualValidatorService
{
    /// <summary>
    /// Dictionary of validation rules keyed by the validated type.
    /// </summary>
    public IDictionary<Type, List<Func<object, bool>>> Rules { get; }

    public ManualValidatorService() : this(new Dictionary<Type, List<Func<object, bool>>>())
    {
    }

    public ManualValidatorService(IDictionary<Type, List<Func<object, bool>>> rules)
    {
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
    }

    /// <inheritdoc />
    public bool Validate(object instance)
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        var type = instance.GetType();
        if (!Rules.TryGetValue(type, out var rules) || rules.Count == 0)
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
