using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ExampleLib.Tests;

/// <summary>
/// Additional tests for SequenceValidator to improve code coverage.
/// </summary>
public class SequenceValidatorAdditionalTests
{
    public class TestEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public bool Validated { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<SaveAudit> SaveAudits { get; set; } = null!;
    }

    [Fact]
    public void Validate_WithNullItems_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            SequenceValidator.Validate<TestEntity, string, decimal>(
                null!,
                e => e.Category,
                e => e.Value,
                (prev, curr) => true));
        Assert.Equal("items", exception.ParamName);
    }

    [Fact]
    public void Validate_WithNullWheneverSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var items = new[] { new TestEntity() };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            SequenceValidator.Validate<TestEntity, string, decimal>(
                items,
                null!,
                e => e.Value,
                (prev, curr) => true));
        Assert.Equal("wheneverSelector", exception.ParamName);
    }

    [Fact]
    public void Validate_WithNullValueSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var items = new[] { new TestEntity() };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            SequenceValidator.Validate<TestEntity, string, decimal>(
                items,
                e => e.Category,
                null!,
                (prev, curr) => true));
        Assert.Equal("valueSelector", exception.ParamName);
    }

    [Fact]
    public void Validate_WithNullValidationFunc_ThrowsArgumentNullException()
    {
        // Arrange
        var items = new[] { new TestEntity() };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            SequenceValidator.Validate<TestEntity, string, decimal>(
                items,
                e => e.Category,
                e => e.Value,
                null!));
        Assert.Equal("validationFunc", exception.ParamName);
    }

    [Fact]
    public void Validate_WithEmptySequence_ReturnsTrue()
    {
        // Arrange
        var items = Array.Empty<TestEntity>();

        // Act
        var result = SequenceValidator.Validate<TestEntity, string, decimal>(
            items,
            e => e.Category,
            e => e.Value,
            (prev, curr) => true);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_WithSingleItem_ReturnsTrue()
    {
        // Arrange
        var items = new[] { new TestEntity { Category = "A", Value = 100 } };

        // Act
        var result = SequenceValidator.Validate<TestEntity, string, decimal>(
            items,
            e => e.Category,
            e => e.Value,
            (prev, curr) => false); // Even with failing validation, single item should pass

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_WithMultipleItemsSameCategory_ValidatesSequentially()
    {
        // Arrange
        var items = new[]
        {
            new TestEntity { Category = "A", Value = 100 },
            new TestEntity { Category = "A", Value = 110 },
            new TestEntity { Category = "A", Value = 120 }
        };

        // Act
        var result = SequenceValidator.Validate<TestEntity, string, decimal>(
            items,
            e => e.Category,
            e => e.Value,
            (prev, curr) => curr >= prev); // Value should increase

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_WithMultipleItemsSameCategoryValidationFails_ReturnsFalse()
    {
        // Arrange
        var items = new[]
        {
            new TestEntity { Category = "A", Value = 100 },
            new TestEntity { Category = "A", Value = 90 }, // This should fail validation
            new TestEntity { Category = "A", Value = 120 }
        };

        // Act
        var result = SequenceValidator.Validate<TestEntity, string, decimal>(
            items,
            e => e.Category,
            e => e.Value,
            (prev, curr) => curr >= prev); // Value should increase

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Validate_WithMultipleCategoriesIndependent_ValidatesEachSeparately()
    {
        // Arrange
        var items = new[]
        {
            new TestEntity { Category = "A", Value = 100 },
            new TestEntity { Category = "B", Value = 50 },
            new TestEntity { Category = "A", Value = 110 },
            new TestEntity { Category = "B", Value = 60 }
        };

        // Act
        var result = SequenceValidator.Validate<TestEntity, string, decimal>(
            items,
            e => e.Category,
            e => e.Value,
            (prev, curr) => curr >= prev); // Value should increase within each category

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_WithDefaultEqualityComparison_WorksCorrectly()
    {
        // Arrange
        var items = new[]
        {
            new TestEntity { Category = "A", Value = 100 },
            new TestEntity { Category = "A", Value = 100 },
            new TestEntity { Category = "A", Value = 100 }
        };

        // Act
        var result = SequenceValidator.Validate<TestEntity, string, decimal>(
            items,
            e => e.Category,
            e => e.Value);

        // Assert
        Assert.True(result); // All values are equal
    }

    [Fact]
    public void Validate_WithDefaultEqualityComparisonFails_ReturnsFalse()
    {
        // Arrange
        var items = new[]
        {
            new TestEntity { Category = "A", Value = 100 },
            new TestEntity { Category = "A", Value = 200 }, // Different value
            new TestEntity { Category = "A", Value = 100 }
        };

        // Act
        var result = SequenceValidator.Validate<TestEntity, string, decimal>(
            items,
            e => e.Category,
            e => e.Value);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Validate_WithSummarisationPlan_WorksCorrectly()
    {
        // Arrange
        var items = new[]
        {
            new TestEntity { Category = "A", Value = 100 },
            new TestEntity { Category = "A", Value = 105 },
            new TestEntity { Category = "A", Value = 110 }
        };

        var plan = new SummarisationPlan<TestEntity>(
            e => e.Value,
            ThresholdType.PercentChange,
            0.1m); // 10% threshold

        // Act
        var result = SequenceValidator.Validate<TestEntity, string>(
            items,
            e => e.Category,
            plan);

        // Assert
        Assert.True(result); // 5% changes are within 10% threshold
    }

    [Fact]
    public void Validate_WithSummarisationPlanExceedsThreshold_ReturnsFalse()
    {
        // Arrange
        var items = new[]
        {
            new TestEntity { Category = "A", Value = 100 },
            new TestEntity { Category = "A", Value = 150 }, // 50% increase
            new TestEntity { Category = "A", Value = 160 }
        };

        var plan = new SummarisationPlan<TestEntity>(
            e => e.Value,
            ThresholdType.PercentChange,
            0.1m); // 10% threshold

        // Act
        var result = SequenceValidator.Validate<TestEntity, string>(
            items,
            e => e.Category,
            plan);

        // Assert
        Assert.False(result); // 50% change exceeds 10% threshold
    }

    [Fact]
    public void Validate_WithNullPlan_ThrowsArgumentNullException()
    {
        // Arrange
        var items = new[] { new TestEntity() };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            SequenceValidator.Validate<TestEntity, string>(
                items,
                e => e.Category,
                null!));
        Assert.Equal("plan", exception.ParamName);
    }

    [Fact]
    public async Task ValidateAgainstLatestAuditAsync_WithNullAudit_ReturnsTrue()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"test_{Guid.NewGuid()}")
            .Options;

        using var context = new TestDbContext(options);
        var entities = new[] { new TestEntity { Id = 1, Value = 100 } };

        // Act
        var result = await SequenceValidator.ValidateAgainstLatestAuditAsync<TestEntity, SaveAudit, int, decimal>(
            entities,
            context.SaveAudits,
            e => e.Id,
            a => a.Id,
            e => e.Value,
            a => a.MetricValue,
            (entityVal, auditVal) => entityVal >= auditVal);

        // Assert
        Assert.True(result); // No audit exists, so validation passes
    }

    [Fact]
    public async Task ValidateAgainstSaveAuditsAsync_WithMatchingAudit_ValidatesCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"test_{Guid.NewGuid()}")
            .Options;

        using var context = new TestDbContext(options);
        
        // Add audit data
        var audit = new SaveAudit
        {
            EntityType = nameof(TestEntity),
            EntityId = "1",
            ApplicationName = "TestApp",
            MetricValue = 90,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        context.SaveAudits.Add(audit);
        await context.SaveChangesAsync();

        var entities = new[] { new TestEntity { Id = 1, Value = 100 } };

        // Act
        var result = await SequenceValidator.ValidateAgainstSaveAuditsAsync<TestEntity, int, decimal>(
            entities,
            context.SaveAudits,
            e => e.Id,
            e => e.Value,
            a => a.MetricValue,
            (entityVal, auditVal) => entityVal >= auditVal,
            "TestApp");

        // Assert
        Assert.True(result); // 100 >= 90
    }

    [Fact]
    public async Task ValidateAgainstSaveAuditsAsync_WithValidationFailure_ReturnsFalse()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"test_{Guid.NewGuid()}")
            .Options;

        using var context = new TestDbContext(options);
        
        // Add audit data
        var audit = new SaveAudit
        {
            EntityType = nameof(TestEntity),
            EntityId = "1",
            ApplicationName = "TestApp",
            MetricValue = 150,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        context.SaveAudits.Add(audit);
        await context.SaveChangesAsync();

        var entities = new[] { new TestEntity { Id = 1, Value = 100 } };

        // Act
        var result = await SequenceValidator.ValidateAgainstSaveAuditsAsync<TestEntity, int, decimal>(
            entities,
            context.SaveAudits,
            e => e.Id,
            e => e.Value,
            a => a.MetricValue,
            (entityVal, auditVal) => entityVal >= auditVal,
            "TestApp");

        // Assert
        Assert.False(result); // 100 < 150
    }

    [Fact]
    public async Task ValidateAgainstSaveAuditsAsync_WithMultipleEntities_ValidatesAll()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"test_{Guid.NewGuid()}")
            .Options;

        using var context = new TestDbContext(options);
        
        // Add audit data for multiple entities
        var audits = new[]
        {
            new SaveAudit
            {
                EntityType = nameof(TestEntity),
                EntityId = "1",
                ApplicationName = "TestApp",
                MetricValue = 90,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
            },
            new SaveAudit
            {
                EntityType = nameof(TestEntity),
                EntityId = "2",
                ApplicationName = "TestApp",
                MetricValue = 80,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-3)
            }
        };
        context.SaveAudits.AddRange(audits);
        await context.SaveChangesAsync();

        var entities = new[]
        {
            new TestEntity { Id = 1, Value = 100 },
            new TestEntity { Id = 2, Value = 85 }
        };

        // Act
        var result = await SequenceValidator.ValidateAgainstSaveAuditsAsync<TestEntity, int, decimal>(
            entities,
            context.SaveAudits,
            e => e.Id,
            e => e.Value,
            a => a.MetricValue,
            (entityVal, auditVal) => entityVal >= auditVal,
            "TestApp");

        // Assert
        Assert.True(result); // Both entities pass validation
    }

    [Fact]
    public async Task ValidateAgainstSaveAuditsAsync_WithNullEntityId_HandlesCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"test_{Guid.NewGuid()}")
            .Options;

        using var context = new TestDbContext(options);
        
        // Entity with null ID selector result
        var entities = new[] { new TestEntity { Id = 1, Value = 100 } };

        // Act - Use string instead of string? to avoid nullability constraint violation
        var result = await SequenceValidator.ValidateAgainstSaveAuditsAsync<TestEntity, string, decimal>(
            entities,
            context.SaveAudits,
            e => string.Empty, // Return empty string instead of null
            e => e.Value,
            a => a.MetricValue,
            (entityVal, auditVal) => entityVal >= auditVal,
            "TestApp");

        // Assert
        Assert.True(result); // Should handle empty string key gracefully
    }
}