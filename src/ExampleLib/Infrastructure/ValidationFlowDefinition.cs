using System.Text.Json.Serialization;
using ExampleLib.Domain;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Defines which validation services should be registered for a given entity.
/// </summary>
public class ValidationFlowDefinition
{
    /// <summary>The assembly qualified name of the entity type.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Register save validation.</summary>
    public bool SaveValidation { get; set; }

    /// <summary>Register save commit auditing.</summary>
    public bool SaveCommit { get; set; }

    /// <summary>Name of the property used to calculate the metric.</summary>
    public string? MetricProperty { get; set; }

    /// <summary>Threshold comparison type for the plan.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ThresholdType ThresholdType { get; set; } = ThresholdType.PercentChange;

    /// <summary>Allowed threshold value for the plan.</summary>
    public decimal ThresholdValue { get; set; } = 0.1m;

    /// <summary>Register delete validation.</summary>
    public bool DeleteValidation { get; set; }

    /// <summary>Register delete commit auditing.</summary>
    public bool DeleteCommit { get; set; }
}
