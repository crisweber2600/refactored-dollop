namespace ExampleLib.Domain;

/// <summary>
/// Provides the name of the running application for auditing purposes.
/// </summary>
public interface IApplicationNameProvider
{
    /// <summary>The current application name.</summary>
    string ApplicationName { get; }
}
