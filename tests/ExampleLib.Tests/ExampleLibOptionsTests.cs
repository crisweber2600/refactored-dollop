using ExampleLib.Infrastructure;

namespace ExampleLib.Tests;

/// <summary>
/// Tests for ExampleLibOptions and related configuration classes to improve code coverage.
/// </summary>
public class ExampleLibOptionsTests
{
    [Fact]
    public void ExampleLibOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new ExampleLibOptions();

        // Assert
        Assert.Equal("DefaultApp", options.ApplicationName);
        Assert.False(options.UseMongoDb);
        Assert.Equal(ExampleLib.Domain.ThresholdType.PercentChange, options.DefaultThresholdType);
        Assert.Equal(0.1m, options.DefaultThresholdValue);
        Assert.NotNull(options.EntityIdProvider);
        Assert.NotNull(options.MongoDb);
        Assert.NotNull(options.EntityFramework);
    }

    [Fact]
    public void ExampleLibOptions_CanSetAllProperties()
    {
        // Arrange
        var options = new ExampleLibOptions
        {
            ApplicationName = "TestApp",
            UseMongoDb = true,
            DefaultThresholdType = ExampleLib.Domain.ThresholdType.RawDifference,
            DefaultThresholdValue = 5.0m,
            EntityIdProvider = new EntityIdProviderOptions
            {
                Type = EntityIdProviderType.Configurable,
                PropertyPriority = new[] { "CustomId", "CustomName" }
            },
            MongoDb = new MongoDbOptions
            {
                DefaultDatabaseName = "CustomDb",
                CollectionNamingStrategy = type => $"Custom_{type.Name}_Collection"
            },
            EntityFramework = new EntityFrameworkOptions
            {
                AutoRegisterRepositories = true
            }
        };

        // Act & Assert
        Assert.Equal("TestApp", options.ApplicationName);
        Assert.True(options.UseMongoDb);
        Assert.Equal(ExampleLib.Domain.ThresholdType.RawDifference, options.DefaultThresholdType);
        Assert.Equal(5.0m, options.DefaultThresholdValue);
        Assert.Equal(EntityIdProviderType.Configurable, options.EntityIdProvider.Type);
        Assert.Equal(new[] { "CustomId", "CustomName" }, options.EntityIdProvider.PropertyPriority);
        Assert.Equal("CustomDb", options.MongoDb.DefaultDatabaseName);
        Assert.Equal("Custom_TestEntity_Collection", options.MongoDb.CollectionNamingStrategy(typeof(TestEntity)));
        Assert.True(options.EntityFramework.AutoRegisterRepositories);
    }

    [Fact]
    public void EntityIdProviderOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new EntityIdProviderOptions();

        // Assert
        Assert.Equal(EntityIdProviderType.Reflection, options.Type);
        Assert.Equal(new[] { "Name", "Code", "Key", "Identifier", "Title", "Label" }, options.PropertyPriority);
    }

    [Fact]
    public void EntityIdProviderOptions_CanSetAllProperties()
    {
        // Arrange & Act
        var options = new EntityIdProviderOptions
        {
            Type = EntityIdProviderType.Default,
            PropertyPriority = new[] { "CustomProperty1", "CustomProperty2" }
        };

        // Assert
        Assert.Equal(EntityIdProviderType.Default, options.Type);
        Assert.Equal(new[] { "CustomProperty1", "CustomProperty2" }, options.PropertyPriority);
    }

    [Fact]
    public void EntityIdProviderType_HasAllExpectedValues()
    {
        // Act & Assert
        var values = Enum.GetValues<EntityIdProviderType>();
        
        Assert.Contains(EntityIdProviderType.Reflection, values);
        Assert.Contains(EntityIdProviderType.Configurable, values);
        Assert.Contains(EntityIdProviderType.Default, values);
        Assert.Equal(3, values.Length);
    }

    [Fact]
    public void MongoDbOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new MongoDbOptions();

        // Assert
        Assert.Equal("ExampleLibDb", options.DefaultDatabaseName);
        Assert.NotNull(options.CollectionNamingStrategy);
        Assert.Equal("TestEntitys", options.CollectionNamingStrategy(typeof(TestEntity)));
    }

    [Fact]
    public void MongoDbOptions_CanSetAllProperties()
    {
        // Arrange & Act
        var options = new MongoDbOptions
        {
            DefaultDatabaseName = "MyDatabase",
            CollectionNamingStrategy = type => $"tbl_{type.Name}"
        };

        // Assert
        Assert.Equal("MyDatabase", options.DefaultDatabaseName);
        Assert.Equal("tbl_TestEntity", options.CollectionNamingStrategy(typeof(TestEntity)));
    }

    [Fact]
    public void MongoDbOptions_DefaultCollectionNamingStrategy_WorksCorrectly()
    {
        // Arrange
        var options = new MongoDbOptions();

        // Act & Assert
        Assert.Equal("Strings", options.CollectionNamingStrategy(typeof(string)));
        Assert.Equal("Int32s", options.CollectionNamingStrategy(typeof(int)));
        Assert.Equal("TestEntitys", options.CollectionNamingStrategy(typeof(TestEntity)));
    }

    [Fact]
    public void EntityFrameworkOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new EntityFrameworkOptions();

        // Assert
        Assert.False(options.AutoRegisterRepositories);
    }

    [Fact]
    public void EntityFrameworkOptions_CanSetAllProperties()
    {
        // Arrange & Act
        var options = new EntityFrameworkOptions
        {
            AutoRegisterRepositories = true
        };

        // Assert
        Assert.True(options.AutoRegisterRepositories);
    }

    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}