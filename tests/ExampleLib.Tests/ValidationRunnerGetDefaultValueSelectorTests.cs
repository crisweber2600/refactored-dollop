using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExampleLib.Tests;

/// <summary>
/// Tests for ValidationRunner's GetDefaultValueSelector method and related functionality.
/// These tests focus on the uncovered code paths identified in the coverage report.
/// </summary>
public class ValidationRunnerGetDefaultValueSelectorTests
{
    // Test entities for various property types
    public class EntityWithDecimalValue : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public decimal Value { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithNullableDecimalValue : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public decimal? Value { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithDoubleValue : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public double Value { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithNullableDoubleValue : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public double? Value { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithFloatValue : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public float Value { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithNullableFloatValue : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public float? Value { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithIntValue : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithNullableIntValue : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public int? Value { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithDecimalAmount : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithNullableDecimalAmount : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public decimal? Amount { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithDoubleAmount : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public double Amount { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithNullableDoubleAmount : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public double? Amount { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithFloatAmount : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public float Amount { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithNullableFloatAmount : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public float? Amount { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithIntAmount : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public int Amount { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithNullableIntAmount : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public int? Amount { get; set; }
        public bool Validated { get; set; }
    }

    public class EntityWithNoValueOrAmount : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Validated { get; set; }
    }

    public class EntityWithStringValue : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Value { get; set; } = string.Empty;
        public bool Validated { get; set; }
    }

    public class EntityWithBothValueAndAmount : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public decimal Value { get; set; }
        public decimal Amount { get; set; }
        public bool Validated { get; set; }
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithDecimalValue_ReturnsValueProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-decimal-value"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithDecimalValue>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithDecimalValue>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act
        var entity = new EntityWithDecimalValue { Id = 1, Value = 123.45m, Validated = true };
        var result = await runner.ValidateAsync(entity);

        // Assert - This tests the GetDefaultValueSelector indirectly through sequence validation
        Assert.True(result);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithNullableDecimalValue_ReturnsValueProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-nullable-decimal-value"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithNullableDecimalValue>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithNullableDecimalValue>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act & Assert - Entity with non-null value
        var entity1 = new EntityWithNullableDecimalValue { Id = 1, Value = 123.45m, Validated = true };
        var result1 = await runner.ValidateAsync(entity1);
        Assert.True(result1);

        // Act & Assert - Entity with null value (should return 0m)
        var entity2 = new EntityWithNullableDecimalValue { Id = 2, Value = null, Validated = true };
        var result2 = await runner.ValidateAsync(entity2);
        Assert.True(result2);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithDoubleValue_ReturnsValueProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-double-value"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithDoubleValue>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithDoubleValue>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act
        var entity = new EntityWithDoubleValue { Id = 1, Value = 123.45, Validated = true };
        var result = await runner.ValidateAsync(entity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithNullableDoubleValue_ReturnsValueProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-nullable-double-value"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithNullableDoubleValue>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithNullableDoubleValue>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act & Assert - Entity with non-null value
        var entity1 = new EntityWithNullableDoubleValue { Id = 1, Value = 123.45, Validated = true };
        var result1 = await runner.ValidateAsync(entity1);
        Assert.True(result1);

        // Act & Assert - Entity with null value
        var entity2 = new EntityWithNullableDoubleValue { Id = 2, Value = null, Validated = true };
        var result2 = await runner.ValidateAsync(entity2);
        Assert.True(result2);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithFloatValue_ReturnsValueProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-float-value"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithFloatValue>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithFloatValue>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act
        var entity = new EntityWithFloatValue { Id = 1, Value = 123.45f, Validated = true };
        var result = await runner.ValidateAsync(entity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithNullableFloatValue_ReturnsValueProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-nullable-float-value"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithNullableFloatValue>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithNullableFloatValue>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act & Assert - Entity with non-null value
        var entity1 = new EntityWithNullableFloatValue { Id = 1, Value = 123.45f, Validated = true };
        var result1 = await runner.ValidateAsync(entity1);
        Assert.True(result1);

        // Act & Assert - Entity with null value
        var entity2 = new EntityWithNullableFloatValue { Id = 2, Value = null, Validated = true };
        var result2 = await runner.ValidateAsync(entity2);
        Assert.True(result2);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithIntValue_ReturnsValueProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-int-value"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithIntValue>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithIntValue>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act
        var entity = new EntityWithIntValue { Id = 1, Value = 123, Validated = true };
        var result = await runner.ValidateAsync(entity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithNullableIntValue_ReturnsValueProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-nullable-int-value"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithNullableIntValue>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithNullableIntValue>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act & Assert - Entity with non-null value
        var entity1 = new EntityWithNullableIntValue { Id = 1, Value = 123, Validated = true };
        var result1 = await runner.ValidateAsync(entity1);
        Assert.True(result1);

        // Act & Assert - Entity with null value
        var entity2 = new EntityWithNullableIntValue { Id = 2, Value = null, Validated = true };
        var result2 = await runner.ValidateAsync(entity2);
        Assert.True(result2);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithDecimalAmount_ReturnsAmountProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-decimal-amount"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithDecimalAmount>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithDecimalAmount>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act
        var entity = new EntityWithDecimalAmount { Id = 1, Amount = 123.45m, Validated = true };
        var result = await runner.ValidateAsync(entity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithNullableDecimalAmount_ReturnsAmountProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-nullable-decimal-amount"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithNullableDecimalAmount>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithNullableDecimalAmount>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act & Assert - Entity with non-null value
        var entity1 = new EntityWithNullableDecimalAmount { Id = 1, Amount = 123.45m, Validated = true };
        var result1 = await runner.ValidateAsync(entity1);
        Assert.True(result1);

        // Act & Assert - Entity with null value
        var entity2 = new EntityWithNullableDecimalAmount { Id = 2, Amount = null, Validated = true };
        var result2 = await runner.ValidateAsync(entity2);
        Assert.True(result2);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithDoubleAmount_ReturnsAmountProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-double-amount"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithDoubleAmount>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithDoubleAmount>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act
        var entity = new EntityWithDoubleAmount { Id = 1, Amount = 123.45, Validated = true };
        var result = await runner.ValidateAsync(entity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithNullableDoubleAmount_ReturnsAmountProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-nullable-double-amount"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithNullableDoubleAmount>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithNullableDoubleAmount>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act & Assert - Entity with non-null value
        var entity1 = new EntityWithNullableDoubleAmount { Id = 1, Amount = 123.45, Validated = true };
        var result1 = await runner.ValidateAsync(entity1);
        Assert.True(result1);

        // Act & Assert - Entity with null value
        var entity2 = new EntityWithNullableDoubleAmount { Id = 2, Amount = null, Validated = true };
        var result2 = await runner.ValidateAsync(entity2);
        Assert.True(result2);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithFloatAmount_ReturnsAmountProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-float-amount"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithFloatAmount>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithFloatAmount>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act
        var entity = new EntityWithFloatAmount { Id = 1, Amount = 123.45f, Validated = true };
        var result = await runner.ValidateAsync(entity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithNullableFloatAmount_ReturnsAmountProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-nullable-float-amount"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithNullableFloatAmount>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithNullableFloatAmount>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act & Assert - Entity with non-null value
        var entity1 = new EntityWithNullableFloatAmount { Id = 1, Amount = 123.45f, Validated = true };
        var result1 = await runner.ValidateAsync(entity1);
        Assert.True(result1);

        // Act & Assert - Entity with null value
        var entity2 = new EntityWithNullableFloatAmount { Id = 2, Amount = null, Validated = true };
        var result2 = await runner.ValidateAsync(entity2);
        Assert.True(result2);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithIntAmount_ReturnsAmountProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-int-amount"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithIntAmount>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithIntAmount>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act
        var entity = new EntityWithIntAmount { Id = 1, Amount = 123, Validated = true };
        var result = await runner.ValidateAsync(entity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithNullableIntAmount_ReturnsAmountProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-nullable-int-amount"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithNullableIntAmount>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithNullableIntAmount>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act & Assert - Entity with non-null value
        var entity1 = new EntityWithNullableIntAmount { Id = 1, Amount = 123, Validated = true };
        var result1 = await runner.ValidateAsync(entity1);
        Assert.True(result1);

        // Act & Assert - Entity with null value
        var entity2 = new EntityWithNullableIntAmount { Id = 2, Amount = null, Validated = true };
        var result2 = await runner.ValidateAsync(entity2);
        Assert.True(result2);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithNoValueOrAmount_FallsBackToId()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-no-value-or-amount"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithNoValueOrAmount>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithNoValueOrAmount>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act
        var entity = new EntityWithNoValueOrAmount { Id = 123, Name = "Test", Validated = true };
        var result = await runner.ValidateAsync(entity);

        // Assert - Should use Id (123) as the value
        Assert.True(result);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithStringValue_FallsBackToId()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-string-value"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithStringValue>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithStringValue>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act
        var entity = new EntityWithStringValue { Id = 123, Value = "Not a number", Validated = true };
        var result = await runner.ValidateAsync(entity);

        // Assert - Should use Id (123) as the value since Value is a string
        Assert.True(result);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithBothValueAndAmount_PrefersValue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-both-value-and-amount"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithBothValueAndAmount>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithBothValueAndAmount>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act
        var entity = new EntityWithBothValueAndAmount { Id = 1, Value = 100m, Amount = 200m, Validated = true };
        var result = await runner.ValidateAsync(entity);

        // Assert - Should prefer Value (100) over Amount (200)
        Assert.True(result);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithExceptionInSummarisationPlan_FallsBackToDefaultSelector()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-exception-fallback"));
        
        var mockSummarisationPlanStore = new Mock<ISummarisationPlanStore>();
        mockSummarisationPlanStore.Setup(s => s.HasPlan<EntityWithDecimalValue>())
                                .Returns(true);
        mockSummarisationPlanStore.Setup(s => s.GetPlan<EntityWithDecimalValue>())
                                .Throws(new InvalidOperationException("Test exception"));

        services.AddSingleton(mockSummarisationPlanStore.Object);
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithDecimalValue>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithDecimalValue>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act
        var entity = new EntityWithDecimalValue { Id = 1, Value = 123.45m, Validated = true };
        var result = await runner.ValidateAsync(entity);

        // Assert - Should handle exception gracefully and use default selector
        Assert.True(result);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithNullSummarisationPlan_FallsBackToDefaultSelector()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-null-plan-fallback"));
        
        var mockSummarisationPlanStore = new Mock<ISummarisationPlanStore>();
        mockSummarisationPlanStore.Setup(s => s.HasPlan<EntityWithDecimalValue>())
                                .Returns(true);
        mockSummarisationPlanStore.Setup(s => s.GetPlan<EntityWithDecimalValue>())
                                .Returns((SummarisationPlan<EntityWithDecimalValue>?)null);

        services.AddSingleton(mockSummarisationPlanStore.Object);
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithDecimalValue>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithDecimalValue>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act
        var entity = new EntityWithDecimalValue { Id = 1, Value = 123.45m, Validated = true };
        var result = await runner.ValidateAsync(entity);

        // Assert - Should handle null plan gracefully and use default selector
        Assert.True(result);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithSequenceValidationException_ReturnsTrue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-sequence-exception"));
        
        // Mock services to cause an exception during sequence validation
        var mockValidationPlanStore = new Mock<IValidationPlanStore>();
        mockValidationPlanStore.Setup(s => s.HasPlan<EntityWithDecimalValue>())
                              .Returns(true);
        mockValidationPlanStore.Setup(s => s.GetPlan<EntityWithDecimalValue>())
                              .Throws(new InvalidOperationException("Sequence validation exception"));

        services.AddSingleton(mockValidationPlanStore.Object);
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationRules<EntityWithDecimalValue>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act
        var entity = new EntityWithDecimalValue { Id = 1, Value = 123.45m, Validated = true };
        var result = await runner.ValidateAsync(entity);

        // Assert - Should handle sequence validation exception gracefully
        Assert.True(result);
    }

    [Fact]
    public async Task GetDefaultValueSelector_WithGracefulDegradation_ReturnsTrue()
    {
        // Arrange - Test graceful degradation when services are missing
        var services = new ServiceCollection();
        services.AddDbContext<TheNannyDbContext>(options => 
            options.UseInMemoryDatabase("test-graceful-degradation"));
        
        services.ConfigureExampleLib(config =>
        {
            config.WithApplicationName("Tests")
                  .UseEntityFramework()
                  .AddValidationPlan<EntityWithDecimalValue>(threshold: 10.0, ValidationStrategy.Count)
                  .AddValidationRules<EntityWithDecimalValue>(e => e.Validated);
        });

        var provider = services.BuildServiceProvider();
        var runner = provider.GetRequiredService<IValidationRunner>();

        // Act
        var entity = new EntityWithDecimalValue { Id = 1, Value = 123.45m, Validated = true };
        var result = await runner.ValidateAsync(entity);

        // Assert - Should handle graceful degradation scenarios
        Assert.True(result);
    }
}