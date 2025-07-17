using ExampleLib.Infrastructure;
using ExampleLib.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleLib.Tests;

/// <summary>
/// Tests for EntityIdProvider implementations.
/// </summary>
public class EntityIdProviderTests
{
    private class TestEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool Validated { get; set; }
    }

    private class EntityWithoutStringProperties : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public bool Validated { get; set; }
    }

    private class EntityWithNullableProperties : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
        public bool Validated { get; set; }
    }

    #region ConfigurableEntityIdProvider Tests

    [Fact]
    public void ConfigurableEntityIdProvider_GetEntityId_WithRegisteredSelector_ReturnsCorrectValue()
    {
        // Arrange
        var provider = new ConfigurableEntityIdProvider();
        provider.RegisterSelector<TestEntity>(e => e.Name);

        var entity = new TestEntity { Id = 1, Name = "TestName", Code = "TestCode" };

        // Act
        var result = provider.GetEntityId(entity);

        // Assert
        Assert.Equal("TestName", result);
    }

    [Fact]
    public void ConfigurableEntityIdProvider_GetEntityId_WithoutRegisteredSelector_ThrowsException()
    {
        // Arrange
        var provider = new ConfigurableEntityIdProvider();
        var entity = new TestEntity { Id = 1, Name = "TestName" };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            provider.GetEntityId(entity));
        Assert.Contains("No selector registered", exception.Message);
    }

    [Fact]
    public void ConfigurableEntityIdProvider_RegisterSelector_WithNullSelector_ThrowsException()
    {
        // Arrange
        var provider = new ConfigurableEntityIdProvider();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            provider.RegisterSelector<TestEntity>(null!));
    }

    [Fact]
    public void ConfigurableEntityIdProvider_RegisterSelector_OverridesPreviousSelector()
    {
        // Arrange
        var provider = new ConfigurableEntityIdProvider();
        provider.RegisterSelector<TestEntity>(e => e.Name);
        provider.RegisterSelector<TestEntity>(e => e.Code);

        var entity = new TestEntity { Id = 1, Name = "TestName", Code = "TestCode" };

        // Act
        var result = provider.GetEntityId(entity);

        // Assert
        Assert.Equal("TestCode", result);
    }

    [Fact]
    public void ConfigurableEntityIdProvider_RegisterSelector_WorksWithMultipleTypes()
    {
        // Arrange
        var provider = new ConfigurableEntityIdProvider();
        provider.RegisterSelector<TestEntity>(e => e.Name);
        provider.RegisterSelector<EntityWithNullableProperties>(e => e.Code ?? "default");

        var entity1 = new TestEntity { Id = 1, Name = "TestName" };
        var entity2 = new EntityWithNullableProperties { Id = 2, Code = "TestCode" };

        // Act
        var result1 = provider.GetEntityId(entity1);
        var result2 = provider.GetEntityId(entity2);

        // Assert
        Assert.Equal("TestName", result1);
        Assert.Equal("TestCode", result2);
    }

    [Fact]
    public void ConfigurableEntityIdProvider_GetEntityId_WithNullEntity_ThrowsException()
    {
        // Arrange
        var provider = new ConfigurableEntityIdProvider();
        provider.RegisterSelector<TestEntity>(e => e.Name);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            provider.GetEntityId<TestEntity>(null!));
    }

    #endregion

    #region ReflectionBasedEntityIdProvider Tests

    [Fact]
    public void ReflectionBasedEntityIdProvider_GetEntityId_WithDefaultPriority_ReturnsNameFirst()
    {
        // Arrange
        var provider = new ReflectionBasedEntityIdProvider();
        var entity = new TestEntity { Id = 1, Name = "TestName", Code = "TestCode" };

        // Act
        var result = provider.GetEntityId(entity);

        // Assert
        Assert.Equal("TestName", result);
    }

    [Fact]
    public void ReflectionBasedEntityIdProvider_GetEntityId_WithCustomPriority_ReturnsInOrder()
    {
        // Arrange
        var provider = new ReflectionBasedEntityIdProvider("Code", "Name");
        var entity = new TestEntity { Id = 1, Name = "TestName", Code = "TestCode" };

        // Act
        var result = provider.GetEntityId(entity);

        // Assert
        Assert.Equal("TestCode", result);
    }

    [Fact]
    public void ReflectionBasedEntityIdProvider_GetEntityId_WithMissingProperty_SkipsToNext()
    {
        // Arrange
        var provider = new ReflectionBasedEntityIdProvider("NonExistent", "Name");
        var entity = new TestEntity { Id = 1, Name = "TestName", Code = "TestCode" };

        // Act
        var result = provider.GetEntityId(entity);

        // Assert
        Assert.Equal("TestName", result);
    }

    [Fact]
    public void ReflectionBasedEntityIdProvider_GetEntityId_WithNullValue_SkipsToNext()
    {
        // Arrange
        var provider = new ReflectionBasedEntityIdProvider("Name", "Code");
        var entity = new EntityWithNullableProperties { Id = 1, Name = null, Code = "TestCode" };

        // Act
        var result = provider.GetEntityId(entity);

        // Assert
        Assert.Equal("TestCode", result);
    }

    [Fact]
    public void ReflectionBasedEntityIdProvider_GetEntityId_WithEmptyString_SkipsToNext()
    {
        // Arrange
        var provider = new ReflectionBasedEntityIdProvider("Name", "Code");
        var entity = new TestEntity { Id = 1, Name = "", Code = "TestCode" };

        // Act
        var result = provider.GetEntityId(entity);

        // Assert
        Assert.Equal("TestCode", result);
    }

    [Fact]
    public void ReflectionBasedEntityIdProvider_GetEntityId_WithWhitespaceString_SkipsToNext()
    {
        // Arrange
        var provider = new ReflectionBasedEntityIdProvider("Name", "Code");
        var entity = new TestEntity { Id = 1, Name = "   ", Code = "TestCode" };

        // Act
        var result = provider.GetEntityId(entity);

        // Assert
        Assert.Equal("TestCode", result);
    }

    [Fact]
    public void ReflectionBasedEntityIdProvider_GetEntityId_WithNoStringProperties_UsesToString()
    {
        // Arrange
        var provider = new ReflectionBasedEntityIdProvider();
        var entity = new EntityWithoutStringProperties { Id = 42, Value = 100 };

        // Act
        var result = provider.GetEntityId(entity);

        // Assert
        Assert.Equal(entity.ToString(), result);
    }

    [Fact]
    public void ReflectionBasedEntityIdProvider_GetEntityId_WithNullEntity_ThrowsException()
    {
        // Arrange
        var provider = new ReflectionBasedEntityIdProvider();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            provider.GetEntityId<TestEntity>(null!));
    }

    [Fact]
    public void ReflectionBasedEntityIdProvider_Constructor_WithNullPriority_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ReflectionBasedEntityIdProvider(null!));
    }

    [Fact]
    public void ReflectionBasedEntityIdProvider_Constructor_WithEmptyPriority_UsesDefault()
    {
        // Arrange
        var provider = new ReflectionBasedEntityIdProvider(Array.Empty<string>());
        var entity = new TestEntity { Id = 1, Name = "TestName", Code = "TestCode" };

        // Act
        var result = provider.GetEntityId(entity);

        // Assert
        Assert.Equal("TestName", result); // Should use default priority
    }

    [Fact]
    public void ReflectionBasedEntityIdProvider_GetEntityId_WithNonStringProperty_SkipsProperty()
    {
        // Arrange
        var provider = new ReflectionBasedEntityIdProvider("Id", "Name"); // Id is int, should be skipped
        var entity = new TestEntity { Id = 1, Name = "TestName", Code = "TestCode" };

        // Act
        var result = provider.GetEntityId(entity);

        // Assert
        Assert.Equal("TestName", result);
    }

    [Fact]
    public void ReflectionBasedEntityIdProvider_GetEntityId_WithCaseSensitiveProperty_MatchesExactly()
    {
        // Arrange
        var provider = new ReflectionBasedEntityIdProvider("name", "Name"); // lowercase should not match
        var entity = new TestEntity { Id = 1, Name = "TestName", Code = "TestCode" };

        // Act
        var result = provider.GetEntityId(entity);

        // Assert
        Assert.Equal("TestName", result); // Should match "Name", not "name"
    }

    [Fact]
    public void ReflectionBasedEntityIdProvider_GetEntityId_WithAllPropertiesInvalid_FallsBackToToString()
    {
        // Arrange
        var provider = new ReflectionBasedEntityIdProvider("NonExistent1", "NonExistent2");
        var entity = new TestEntity { Id = 1, Name = "TestName", Code = "TestCode" };

        // Act
        var result = provider.GetEntityId(entity);

        // Assert
        Assert.Equal(entity.ToString(), result);
    }

    [Fact]
    public void ReflectionBasedEntityIdProvider_GetEntityId_WithSpecialCharacters_ReturnsCorrectValue()
    {
        // Arrange
        var provider = new ReflectionBasedEntityIdProvider("Name");
        var entity = new TestEntity { Id = 1, Name = "Test-Name_With.Special$Chars", Code = "TestCode" };

        // Act
        var result = provider.GetEntityId(entity);

        // Assert
        Assert.Equal("Test-Name_With.Special$Chars", result);
    }

    [Fact]
    public void ReflectionBasedEntityIdProvider_GetEntityId_WithUnicodeCharacters_ReturnsCorrectValue()
    {
        // Arrange
        var provider = new ReflectionBasedEntityIdProvider("Name");
        var entity = new TestEntity { Id = 1, Name = "Test??Name", Code = "TestCode" };

        // Act
        var result = provider.GetEntityId(entity);

        // Assert
        Assert.Equal("Test??Name", result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void EntityIdProviders_WorkCorrectlyWithServiceCollection()
    {
        // Test ConfigurableEntityIdProvider
        var services1 = new ServiceCollection();
        services1.AddConfigurableEntityIdProvider(provider =>
        {
            provider.RegisterSelector<TestEntity>(e => e.Code);
        });

        var serviceProvider1 = services1.BuildServiceProvider();
        var entityIdProvider1 = serviceProvider1.GetService<IEntityIdProvider>();
        Assert.NotNull(entityIdProvider1);
        Assert.IsType<ConfigurableEntityIdProvider>(entityIdProvider1);

        var entity1 = new TestEntity { Code = "TestCode", Name = "TestName" };
        Assert.Equal("TestCode", entityIdProvider1.GetEntityId(entity1));

        // Test ReflectionBasedEntityIdProvider
        var services2 = new ServiceCollection();
        services2.AddReflectionBasedEntityIdProvider("Title", "Name");

        var serviceProvider2 = services2.BuildServiceProvider();
        var entityIdProvider2 = serviceProvider2.GetService<IEntityIdProvider>();
        Assert.NotNull(entityIdProvider2);
        Assert.IsType<ReflectionBasedEntityIdProvider>(entityIdProvider2);

        var entity2 = new TestEntity { Title = "TestTitle", Name = "TestName" };
        Assert.Equal("TestTitle", entityIdProvider2.GetEntityId(entity2));
    }

    [Fact]
    public void EntityIdProviders_WorkWithServiceCollectionRemoval()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IEntityIdProvider, ConfigurableEntityIdProvider>();
        services.AddSingleton<IEntityIdProvider, ReflectionBasedEntityIdProvider>();

        // Act
        services.RemoveAll<IEntityIdProvider>();
        services.AddDefaultEntityIdProvider();

        var provider = services.BuildServiceProvider();

        // Assert
        var entityIdProvider = provider.GetService<IEntityIdProvider>();
        Assert.NotNull(entityIdProvider);
        Assert.IsType<ReflectionBasedEntityIdProvider>(entityIdProvider);
    }

    #endregion
}