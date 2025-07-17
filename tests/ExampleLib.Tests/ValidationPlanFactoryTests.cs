using ExampleLib.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExampleLib.Tests;

/// <summary>
/// Tests for ValidationPlanFactory to improve code coverage.
/// </summary>
public class ValidationPlanFactoryTests
{
    public class TestEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public bool Validated { get; set; }
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        
        public DbSet<TestEntity> TestEntities { get; set; }
    }

    [Fact]
    public void CreatePlans_WithNullConnectionString_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            ValidationPlanFactory.CreatePlans<TestEntity, TestDbContext>(null!));
        Assert.Equal("connectionString", exception.ParamName);
    }

    [Fact]
    public void CreatePlans_WithEmptyConnectionString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            ValidationPlanFactory.CreatePlans<TestEntity, TestDbContext>(""));
        Assert.Equal("connectionString", exception.ParamName);
        Assert.Contains("Connection string cannot be empty or whitespace", exception.Message);
    }

    [Fact]
    public void CreatePlans_WithWhitespaceConnectionString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            ValidationPlanFactory.CreatePlans<TestEntity, TestDbContext>("   "));
        Assert.Equal("connectionString", exception.ParamName);
        Assert.Contains("Connection string cannot be empty or whitespace", exception.Message);
    }

    [Fact]
    public void CreatePlans_WithInvalidConnectionString_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ValidationPlanFactory.CreatePlans<TestEntity, TestDbContext>("invalid"));
        Assert.Contains("Invalid connection string", exception.Message);
    }

    [Fact]
    public void CreatePlans_WithShortConnectionString_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ValidationPlanFactory.CreatePlans<TestEntity, TestDbContext>("short"));
        Assert.Contains("Invalid connection string", exception.Message);
    }

    [Fact]
    public void CreatePlans_WithSqlServerConnectionString_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ValidationPlanFactory.CreatePlans<TestEntity, TestDbContext>("Server=localhost;Database=test;"));
        Assert.Contains("SQL Server connection not available in test environment", exception.Message);
    }

    [Fact]
    public void CreatePlans_WithServerConnectionString_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ValidationPlanFactory.CreatePlans<TestEntity, TestDbContext>("server=localhost;database=test;"));
        Assert.Contains("SQL Server connection not available in test environment", exception.Message);
    }

    [Fact]
    public void CreatePlans_WithInMemoryConnectionString_ReturnsValidationPlans()
    {
        // Arrange
        var connectionString = "Data Source=:memory:;Version=3;";

        // Act
        var plans = ValidationPlanFactory.CreatePlans<TestEntity, TestDbContext>(connectionString);

        // Assert
        Assert.NotNull(plans);
        Assert.NotEmpty(plans);
        
        // Should create plans for all property types of TestEntity
        var propertyTypes = typeof(TestEntity).GetProperties().Select(p => p.PropertyType).Distinct();
        Assert.Equal(propertyTypes.Count(), plans.Count);
    }

    [Fact]
    public void CreatePlans_WithValidConnectionString_CreatesPlansForAllProperties()
    {
        // Arrange
        var connectionString = "Data Source=:memory:;Version=3;New=true;";

        // Act
        var plans = ValidationPlanFactory.CreatePlans<TestEntity, TestDbContext>(connectionString);

        // Assert
        Assert.NotNull(plans);
        Assert.NotEmpty(plans);
        
        // Each plan should have a valid property type
        foreach (var plan in plans)
        {
            Assert.NotNull(plan.PropertyType);
        }
    }

    [Fact]
    public void CreatePlans_WithOtherConnectionString_FallsBackToInMemory()
    {
        // Arrange
        var connectionString = "SomeOtherConnectionString=test;";

        // Act
        var plans = ValidationPlanFactory.CreatePlans<TestEntity, TestDbContext>(connectionString);

        // Assert
        Assert.NotNull(plans);
        Assert.NotEmpty(plans);
        
        // Should still work with in-memory fallback
        Assert.Equal(typeof(TestEntity).GetProperties().Select(p => p.PropertyType).Distinct().Count(), plans.Count);
    }

    public class EntityWithMultipleTypes : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public bool Validated { get; set; }
        public double Price { get; set; }
        public float Rating { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MultiTypeTestDbContext : DbContext
    {
        public MultiTypeTestDbContext(DbContextOptions<MultiTypeTestDbContext> options) : base(options) { }
        
        public DbSet<EntityWithMultipleTypes> Entities { get; set; }
    }

    [Fact]
    public void CreatePlans_WithEntityWithMultipleTypes_CreatesDistinctPlans()
    {
        // Arrange
        var connectionString = "Data Source=:memory:;Version=3;";

        // Act
        var plans = ValidationPlanFactory.CreatePlans<EntityWithMultipleTypes, MultiTypeTestDbContext>(connectionString);

        // Assert
        Assert.NotNull(plans);
        Assert.NotEmpty(plans);
        
        // Should create distinct plans for each unique property type
        var distinctTypes = typeof(EntityWithMultipleTypes).GetProperties().Select(p => p.PropertyType).Distinct();
        Assert.Equal(distinctTypes.Count(), plans.Count);
        
        // Verify all property types are represented
        var planTypes = plans.Select(p => p.PropertyType).ToList();
        foreach (var expectedType in distinctTypes)
        {
            Assert.Contains(expectedType, planTypes);
        }
    }
}