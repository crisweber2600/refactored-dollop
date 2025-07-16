using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleLib.Tests;

/// <summary>
/// Test entities for ValidationRunner testing that mirror WorkerService1 scenarios
/// </summary>
public class TestSampleEntity : IValidatable, IBaseEntity, IRootEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public bool Validated { get; set; }
}

public class TestOtherEntity : IValidatable, IBaseEntity, IRootEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Amount { get; set; }
    public bool IsActive { get; set; }
    public bool Validated { get; set; }
}

public class ValidationRunnerTests
{
    [Fact]
    public async Task ValidateAsync_ReturnsTrue_WhenRulesPass()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IApplicationNameProvider>(new StaticApplicationNameProvider("Tests"));
        services.AddDbContext<YourDbContext>(o => o.UseInMemoryDatabase("valid-pass"));
        
        // Use the new fluent configuration approach
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddSummarisationPlan<YourEntity>(e => e.Id, ThresholdType.RawDifference, 5m)
                  .AddValidationRules<YourEntity>(e => !string.IsNullOrWhiteSpace(e.Name));
        });

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
        
        // Use the new fluent configuration approach
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddSummarisationPlan<YourEntity>(e => e.Id, ThresholdType.RawDifference, 5m)
                  .AddValidationRules<YourEntity>(e => !string.IsNullOrWhiteSpace(e.Name));
        });
        
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
        
        // Use the new fluent configuration approach
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddSummarisationPlan<YourEntity>(e => e.Timestamp.Ticks, ThresholdType.RawDifference, 1m)
                  .AddValidationRules<YourEntity>(e => true);
        });
        
        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        var entity = new YourEntity { Id = 1, Name = "One", Timestamp = DateTime.UtcNow, Validated = true };
        await runner.ValidateAsync(entity);

        entity.Timestamp = entity.Timestamp.AddMinutes(5);
        var result = await runner.ValidateAsync(entity);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAsync_WithSampleEntity_ReturnsTrue_WhenAllValidationsPass()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(o => o.UseInMemoryDatabase("sample-entity-valid"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .WithConfigurableEntityIds(provider =>
                  {
                      provider.RegisterSelector<TestSampleEntity>(entity => entity.Name);
                  })
                  .AddSummarisationPlan<TestSampleEntity>(
                      entity => (decimal)entity.Value,
                      ThresholdType.RawDifference,
                      5.0m)
                  .AddValidationPlan<TestSampleEntity>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<TestSampleEntity>(
                      entity => !string.IsNullOrWhiteSpace(entity.Name),
                      entity => entity.Value >= 0,
                      entity => entity.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        var entity = new TestSampleEntity { Name = "TestSample", Value = 10.0, Validated = true };
        var result = await runner.ValidateAsync(entity);

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAsync_WithOtherEntity_ReturnsFalse_WhenManualValidationFails()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(o => o.UseInMemoryDatabase("other-entity-invalid"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .WithConfigurableEntityIds(provider =>
                  {
                      provider.RegisterSelector<TestOtherEntity>(entity => entity.Code);
                  })
                  .AddSummarisationPlan<TestOtherEntity>(
                      entity => entity.Amount,
                      ThresholdType.RawDifference,
                      10.0m)
                  .AddValidationPlan<TestOtherEntity>(threshold: 5.0, ValidationStrategy.Count)
                  .AddValidationRules<TestOtherEntity>(
                      entity => !string.IsNullOrWhiteSpace(entity.Code),
                      entity => entity.Amount > 0,
                      entity => entity.IsActive);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Test with empty code - should fail manual validation
        var entity = new TestOtherEntity { Code = "", Amount = 100, IsActive = true, Validated = true };
        var result = await runner.ValidateAsync(entity);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAsync_WithSequenceValidation_ReturnsTrue_WhenWithinThreshold()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(o => o.UseInMemoryDatabase("sequence-valid"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .WithConfigurableEntityIds(provider =>
                  {
                      provider.RegisterSelector<TestSampleEntity>(entity => entity.Name);
                  })
                  .AddSummarisationPlan<TestSampleEntity>(
                      entity => (decimal)entity.Value,
                      ThresholdType.RawDifference,
                      10.0m)
                  .AddValidationPlan<TestSampleEntity>(threshold: 5.0, ValidationStrategy.Count)
                  .AddValidationRules<TestSampleEntity>(
                      entity => !string.IsNullOrWhiteSpace(entity.Name),
                      entity => entity.Value >= 0);
        });

        var provider = services.BuildServiceProvider();
        var auditDbContext = provider.GetRequiredService<TheNannyDbContext>();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Seed audit data
        auditDbContext.SaveAudits.Add(new SaveAudit
        {
            EntityType = nameof(TestSampleEntity),
            EntityId = "TestEntity",
            ApplicationName = "Tests",
            MetricValue = 10.0m,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        });
        await auditDbContext.SaveChangesAsync();

        // Test entity with value within threshold (10.0 -> 14.0 = 4.0 difference, threshold is 5.0)
        var entity = new TestSampleEntity { Name = "TestEntity", Value = 14.0, Validated = true };
        var result = await runner.ValidateAsync(entity);

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAsync_WithSequenceValidation_ReturnsFalse_WhenExceedsThreshold()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(o => o.UseInMemoryDatabase("sequence-invalid"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .WithConfigurableEntityIds(provider =>
                  {
                      provider.RegisterSelector<TestSampleEntity>(entity => entity.Name);
                  })
                  .AddSummarisationPlan<TestSampleEntity>(
                      entity => (decimal)entity.Value,
                      ThresholdType.RawDifference,
                      10.0m)
                  .AddValidationPlan<TestSampleEntity>(threshold: 3.0, ValidationStrategy.Count)
                  .AddValidationRules<TestSampleEntity>(
                      entity => !string.IsNullOrWhiteSpace(entity.Name),
                      entity => entity.Value >= 0);
        });

        var provider = services.BuildServiceProvider();
        var auditDbContext = provider.GetRequiredService<TheNannyDbContext>();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Seed audit data
        auditDbContext.SaveAudits.Add(new SaveAudit
        {
            EntityType = nameof(TestSampleEntity),
            EntityId = "TestEntity2",
            ApplicationName = "Tests",
            MetricValue = 10.0m,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        });
        await auditDbContext.SaveChangesAsync();

        // Test entity with value exceeding threshold (10.0 -> 20.0 = 10.0 difference, threshold is 3.0)
        var entity = new TestSampleEntity { Name = "TestEntity2", Value = 20.0, Validated = true };
        var result = await runner.ValidateAsync(entity);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAsync_WithoutValidationPlan_SkipsSequenceValidation()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(o => o.UseInMemoryDatabase("no-validation-plan"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddSummarisationPlan<TestSampleEntity>(
                      entity => (decimal)entity.Value,
                      ThresholdType.RawDifference,
                      10.0m)
                  .AddValidationRules<TestSampleEntity>(
                      entity => !string.IsNullOrWhiteSpace(entity.Name),
                      entity => entity.Value >= 0);
            // Note: No AddValidationPlan call - sequence validation should be skipped
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        var entity = new TestSampleEntity { Name = "TestEntity", Value = 100.0, Validated = true };
        var result = await runner.ValidateAsync(entity);

        // Should pass because sequence validation is skipped when no ValidationPlan is configured
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAsync_WithMissingServices_GracefullySkipsSequenceValidation()
    {
        var services = new ServiceCollection();
        // Intentionally not registering TheNannyDbContext to test graceful degradation
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddSummarisationPlan<TestSampleEntity>(
                      entity => (decimal)entity.Value,
                      ThresholdType.RawDifference,
                      10.0m)
                  .AddValidationPlan<TestSampleEntity>(threshold: 5.0, ValidationStrategy.Count)
                  .AddValidationRules<TestSampleEntity>(
                      entity => !string.IsNullOrWhiteSpace(entity.Name),
                      entity => entity.Value >= 0);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        var entity = new TestSampleEntity { Name = "TestEntity", Value = 10.0, Validated = true };
        var result = await runner.ValidateAsync(entity);

        // Should pass because sequence validation is skipped when required services are missing
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAsync_CombinedValidations_ReturnsFalse_WhenAnyValidationFails()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(o => o.UseInMemoryDatabase("combined-validation"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .WithConfigurableEntityIds(provider =>
                  {
                      provider.RegisterSelector<TestOtherEntity>(entity => entity.Code);
                  })
                  .AddSummarisationPlan<TestOtherEntity>(
                      entity => entity.Amount,
                      ThresholdType.RawDifference,
                      5.0m)
                  .AddValidationPlan<TestOtherEntity>(threshold: 3.0, ValidationStrategy.Count)
                  .AddValidationRules<TestOtherEntity>(
                      entity => !string.IsNullOrWhiteSpace(entity.Code),
                      entity => entity.Amount > 0,
                      entity => entity.IsActive);
        });

        var provider = services.BuildServiceProvider();
        var auditDbContext = provider.GetRequiredService<TheNannyDbContext>();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Seed audit data
        auditDbContext.SaveAudits.Add(new SaveAudit
        {
            EntityType = nameof(TestOtherEntity),
            EntityId = "TestCode",
            ApplicationName = "Tests",
            MetricValue = 100.0m,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        });
        await auditDbContext.SaveChangesAsync();

        // Test entity that passes manual and summarisation validation but fails sequence validation
        var entity = new TestOtherEntity 
        { 
            Code = "TestCode", 
            Amount = 104, // Within summarisation threshold (104-100=4, threshold=5)
            IsActive = true, 
            Validated = true 
        };

        var result = await runner.ValidateAsync(entity);

        // Should fail because sequence validation threshold is exceeded (4 > 3)
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAsync_WithPercentChangeThreshold_WorksCorrectly()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(o => o.UseInMemoryDatabase("percent-change"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .WithConfigurableEntityIds(provider =>
                  {
                      provider.RegisterSelector<TestSampleEntity>(entity => entity.Name);
                  })
                  .AddSummarisationPlan<TestSampleEntity>(
                      entity => (decimal)entity.Value,
                      ThresholdType.PercentChange,
                      0.1m) // 10% change allowed
                  .AddValidationRules<TestSampleEntity>(
                      entity => !string.IsNullOrWhiteSpace(entity.Name),
                      entity => entity.Value >= 0);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // First validation to create audit record
        var firstEntity = new TestSampleEntity { Name = "PercentTest", Value = 100.0, Validated = true };
        await runner.ValidateAsync(firstEntity);

        // Second validation within 10% threshold (100 -> 105 = 5% change)
        var secondEntity = new TestSampleEntity { Name = "PercentTest", Value = 105.0, Validated = true };
        var result = await runner.ValidateAsync(secondEntity);

        Assert.True(result);

        // Third validation exceeding 10% threshold (100 -> 120 = 20% change)
        var thirdEntity = new TestSampleEntity { Name = "PercentTest", Value = 120.0, Validated = true };
        var failResult = await runner.ValidateAsync(thirdEntity);

        Assert.False(failResult);
    }

    [Fact]
    public async Task ValidateAsync_WithMultipleManualRules_AllMustPass()
    {
        var services = new ServiceCollection();
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddSummarisationPlan<TestOtherEntity>(
                      entity => entity.Amount,
                      ThresholdType.RawDifference,
                      100.0m)
                  .AddValidationRules<TestOtherEntity>(
                      entity => !string.IsNullOrWhiteSpace(entity.Code),
                      entity => entity.Amount > 0,
                      entity => entity.IsActive,
                      entity => entity.Amount <= 1000); // Additional rule
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Test entity that fails one manual rule (Amount > 1000)
        var entity = new TestOtherEntity 
        { 
            Code = "ValidCode", 
            Amount = 1500, // Exceeds the <= 1000 rule
            IsActive = true, 
            Validated = true 
        };

        var result = await runner.ValidateAsync(entity);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAsync_WithCancellationToken_HandlesRequestCorrectly()
    {
        var services = new ServiceCollection();
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddSummarisationPlan<YourEntity>(e => e.Id, ThresholdType.RawDifference, 5m)
                  .AddValidationRules<YourEntity>(e => !string.IsNullOrWhiteSpace(e.Name));
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        var entity = new YourEntity { Name = "Valid", Validated = true };
        using var cts = new CancellationTokenSource();

        var result = await runner.ValidateAsync(entity, cts.Token);
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAsync_ExceptionInSequenceValidation_ReturnsTrue()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(o => o.UseInMemoryDatabase("exception-handling"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddSummarisationPlan<TestSampleEntity>(
                      entity => (decimal)entity.Value,
                      ThresholdType.RawDifference,
                      10.0m)
                  .AddValidationPlan<TestSampleEntity>(threshold: 5.0, ValidationStrategy.Count)
                  .AddValidationRules<TestSampleEntity>(
                      entity => !string.IsNullOrWhiteSpace(entity.Name));
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Entity that would cause issues in sequence validation but should gracefully handle exceptions
        var entity = new TestSampleEntity { Name = "ExceptionTest", Value = 10.0, Validated = true };
        var result = await runner.ValidateAsync(entity);

        // Should return true when sequence validation encounters exceptions (graceful degradation)
        Assert.True(result);
    }
}
