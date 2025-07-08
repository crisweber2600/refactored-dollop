using System.Text.Json;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Collection of <see cref="ValidationFlowDefinition"/> instances.
/// Supports loading from JSON where the root may be a single object or an array.
/// </summary>
public class ValidationFlowOptions
{
    public List<ValidationFlowDefinition> Flows { get; set; } = new();

    public static ValidationFlowOptions Load(string json)
    {
        var trimmed = json.TrimStart();
        if (trimmed.StartsWith("["))
        {
            var flows = JsonSerializer.Deserialize<List<ValidationFlowDefinition>>(json);
            return new ValidationFlowOptions { Flows = flows ?? new List<ValidationFlowDefinition>() };
        }
        else
        {
            var flow = JsonSerializer.Deserialize<ValidationFlowDefinition>(json);
            var list = flow != null ? new List<ValidationFlowDefinition> { flow } : new List<ValidationFlowDefinition>();
            return new ValidationFlowOptions { Flows = list };
        }
    }
}
