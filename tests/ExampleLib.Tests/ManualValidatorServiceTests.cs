using ExampleLib.Domain;

namespace ExampleLib.Tests;

public class ManualValidatorServiceTests
{
    [Fact]
    public void Validate_ReturnsTrue_WhenNoRules()
    {
        var service = new ManualValidatorService(new Dictionary<Type, List<Func<object, bool>>>());
        var result = service.Validate(new object());
        Assert.True(result);
    }

    [Fact]
    public void Validate_ReturnsTrue_WhenAllRulesPass()
    {
        var rules = new Dictionary<Type, List<Func<object, bool>>>
        {
            { typeof(object), new List<Func<object, bool>> { _ => true, _ => true } }
        };
        var service = new ManualValidatorService(rules);
        Assert.True(service.Validate(new object()));
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenAnyRuleFails()
    {
        var rules = new Dictionary<Type, List<Func<object, bool>>>
        {
            { typeof(object), new List<Func<object, bool>> { _ => true, _ => false } }
        };
        var service = new ManualValidatorService(rules);
        Assert.False(service.Validate(new object()));
    }
}
