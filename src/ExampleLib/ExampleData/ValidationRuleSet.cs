using System.Linq.Expressions;

namespace ExampleData;

/// <summary>
/// Represents a set of validation rules applied using the same metric selector.
/// </summary>
public class ValidationRuleSet<TEntity> where TEntity : class
{
    /// <summary>Expression selecting the metric used for all rules.</summary>
    public Expression<Func<TEntity, double>> Selector { get; }

    /// <summary>Collection of individual rules.</summary>
    public IReadOnlyList<ValidationRule> Rules { get; }

    public ValidationRuleSet(Expression<Func<TEntity, double>> selector, params ValidationRule[] rules)
    {
        Selector = selector;
        Rules = rules.ToList();
    }
}
