using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleLib.Tests;

public class ValidationRunnerTests
{
    [Fact]
    public async Task ValidateAsync_ReturnsTrue_WhenRulesPass()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IApplicationNameProvider>(new StaticApplicationNameProvider("Tests"));
        services.AddDbContext<YourDbContext>(o => o.UseInMemoryDatabase("valid-pass"));
        services.AddSaveValidation<YourEntity>(e => e.Id, ThresholdType.RawDifference, 5m,
            e => !string.IsNullOrWhiteSpace(e.Name));
        services.AddValidationRunner();

        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<YourDbContext>();
        var repo = new EfGenericRepository<YourEntity>(context);
        var runner = provider.GetRequiredService<IValidationRunner>();

        var entity = new YourEntity { Name = "Valid", Validated = true };
        await repo.AddAsync(entity);
        await provider.GetRequiredService<YourDbContext>().SaveChangesAsync();

        var result = await runner.ValidateAsync(entity);
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsFalse_WhenManualRuleFails()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IApplicationNameProvider>(new StaticApplicationNameProvider("Tests"));
        services.AddDbContext<YourDbContext>(o => o.UseInMemoryDatabase("manual-fail"));
        services.AddSaveValidation<YourEntity>(e => e.Id, ThresholdType.RawDifference, 5m,
            e => !string.IsNullOrWhiteSpace(e.Name));
        services.AddValidationRunner();
        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<YourDbContext>();
        var repo = new EfGenericRepository<YourEntity>(context);
        var runner = provider.GetRequiredService<IValidationRunner>();

        var entity = new YourEntity { Name = "", Validated = true };
        await repo.AddAsync(entity);
        await provider.GetRequiredService<YourDbContext>().SaveChangesAsync();

        var result = await runner.ValidateAsync(entity);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsFalse_WhenSummarisationRuleFails()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IApplicationNameProvider>(new StaticApplicationNameProvider("Tests"));
        services.AddSaveValidation<YourEntity>(e => (decimal)e.Timestamp.Ticks, ThresholdType.RawDifference, 1m,
            e => true);
        services.AddValidationRunner();
        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        var entity = new YourEntity { Id = 1, Name = "One", Timestamp = DateTime.UtcNow, Validated = true };
        await runner.ValidateAsync(entity);

        entity.Timestamp = entity.Timestamp.AddMinutes(5);
        var result = await runner.ValidateAsync(entity);

        Assert.False(result);
    }
}
