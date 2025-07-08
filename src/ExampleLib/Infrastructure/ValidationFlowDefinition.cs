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

    /// <summary>Register delete validation.</summary>
    public bool DeleteValidation { get; set; }

    /// <summary>Register delete commit auditing.</summary>
    public bool DeleteCommit { get; set; }
}
