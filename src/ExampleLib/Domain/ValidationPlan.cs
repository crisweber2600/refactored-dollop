namespace ExampleLib.Domain;

/// <summary>
/// Describes how to validate an entity type using a specific strategy and threshold.
/// </summary>
public class ValidationPlan
{
    /// <summary>The entity type that the plan applies to.</summary>
    public Type EntityType { get; }

    /// <summary>The strategy used when computing the metric.</summary>
    public ValidationStrategy Strategy { get; }

    /// <summary>The threshold that must be met.</summary>
    public double Threshold { get; }

    /// <summary>
    /// Create a new plan using the count strategy by default.
    /// </summary>
    public ValidationPlan(Type entityType, double threshold = 0d, ValidationStrategy strategy = ValidationStrategy.Count)
    {
        EntityType = entityType;
        Strategy = strategy;
        Threshold = threshold;
    }
}
