using ExampleLib.Domain;
using ExampleData;
using ExampleLib.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExampleLib.Tests;

public class ManualValidatorServiceTests
{
    [Fact]
    public void AddValidatorService_RegistersManualValidator()
    {
        var services = new ServiceCollection();
        services.AddValidatorService();
        services.AddValidatorRule<YourEntity>(_ => true);
        var provider = services.BuildServiceProvider();

        var validator = provider.GetService<IManualValidatorService>();
        Assert.NotNull(validator);
    }

    [Fact]
    public void Validate_ReturnsTrue_WhenRulesPass()
    {
        var services = new ServiceCollection();
        services.AddValidatorService();
        services.AddValidatorRule<YourEntity>(_ => true);
        var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IManualValidatorService>();

        var result = validator.Validate(new YourEntity());

        Assert.True(result);
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenRuleFails()
    {
        var services = new ServiceCollection();
        services.AddValidatorService();
        services.AddValidatorRule<YourEntity>(_ => false);
        var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IManualValidatorService>();

        var result = validator.Validate(new YourEntity());

        Assert.False(result);
    }

    [Fact]
    public void Validate_ReturnsTrue_WhenNoRulesExist()
    {
        var services = new ServiceCollection();
        services.AddValidatorService();
        var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IManualValidatorService>();

        var result = validator.Validate(new YourEntity());

        Assert.True(result);
    }
}
