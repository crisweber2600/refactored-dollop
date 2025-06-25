namespace ExampleLib.Domain;

/// <summary>
/// Validates objects using manually registered rules.
/// </summary>
public interface IManualValidatorService
{
    /// <summary>
    /// Validate the provided instance using rules for its runtime type.
    /// Returns <c>true</c> when all rules succeed or no rules exist.
    /// </summary>
    bool Validate(object instance);
}
