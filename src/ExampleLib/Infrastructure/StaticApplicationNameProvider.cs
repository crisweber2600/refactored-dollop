using ExampleLib.Domain;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Simple <see cref="IApplicationNameProvider"/> that returns a fixed value.
/// </summary>
public class StaticApplicationNameProvider : IApplicationNameProvider
{
    public StaticApplicationNameProvider(string applicationName)
    {
        ApplicationName = applicationName;
    }

    public string ApplicationName { get; }
}
