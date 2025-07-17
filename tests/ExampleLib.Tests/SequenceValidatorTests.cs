using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ExampleLib.Tests;

/// <summary>
/// Tests for SequenceValidator to improve code coverage.
/// </summary>
public class SequenceValidatorTests
{
    private class TestEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public bool Validated { get; set; }
    }

    private class TestAudit
    {
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public decimal MetricValue { get; set; }
        public DateTime Timestamp { get; set; }
    }

    [Fact]
    public void Validate_WithNullItems_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            SequenceValidator.Validate<TestEntity, int, decimal>(
                null!, 
                e => e.Id, 
                e => e.Value, 
                (prev, current) => current >= prev));
    }

    [Fact]
    public void Validate_WithNullKeySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new TestEntity { Id = 1, Value = 10m },
            new TestEntity { Id = 2, Value = 20m }
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            SequenceValidator.Validate<TestEntity, int, decimal>(
                items, 
                null!, 
                e => e.Value, 
                (prev, current) => current >= prev));
    }

    [Fact]
    public void Validate_WithNullValueSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new TestEntity { Id = 1, Value = 10m },
            new TestEntity { Id = 2, Value = 20m }
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            SequenceValidator.Validate<TestEntity, int, decimal>(
                items, 
                e => e.Id, 
                null!, 
                (prev, current) => current >= prev));
    }

    [Fact]
    public void Validate_WithNullValidator_ThrowsArgumentNullException()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new TestEntity { Id = 1, Value = 10m },
            new TestEntity { Id = 2, Value = 20m }
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            SequenceValidator.Validate<TestEntity, int, decimal>(
                items, 
                e => e.Id, 
                e => e.Value, 
                null!));
    }

    [Fact]
    public void Validate_WithEmptyItems_ReturnsTrue()
    {
        // Arrange
        var items = new List<TestEntity>();

        // Act
        var result = SequenceValidator.Validate<TestEntity, int, decimal>(
            items, 
            e => e.Id, 
            e => e.Value, 
            (prev, current) => current >= prev);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_WithSingleItem_ReturnsTrue()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new TestEntity { Id = 1, Value = 10m }
        };

        // Act
        var result = SequenceValidator.Validate<TestEntity, int, decimal>(
            items, 
            e => e.Id, 
            e => e.Value, 
            (prev, current) => current >= prev);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_WithValidSequence_ReturnsTrue()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new TestEntity { Id = 1, Value = 10m },
            new TestEntity { Id = 1, Value = 20m },
            new TestEntity { Id = 1, Value = 30m }
        };

        // Act
        var result = SequenceValidator.Validate<TestEntity, int, decimal>(
            items, 
            e => e.Id, 
            e => e.Value, 
            (prev, current) => current >= prev);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_WithInvalidSequence_ReturnsFalse()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new TestEntity { Id = 1, Value = 30m },
            new TestEntity { Id = 1, Value = 20m },
            new TestEntity { Id = 1, Value = 10m }
        };

        // Act
        var result = SequenceValidator.Validate<TestEntity, int, decimal>(
            items, 
            e => e.Id, 
            e => e.Value, 
            (prev, current) => current >= prev);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Validate_WithMultipleGroups_ValidatesEachGroup()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new TestEntity { Id = 1, Value = 10m },
            new TestEntity { Id = 1, Value = 20m },
            new TestEntity { Id = 2, Value = 5m },
            new TestEntity { Id = 2, Value = 15m }
        };

        // Act
        var result = SequenceValidator.Validate<TestEntity, int, decimal>(
            items, 
            e => e.Id, 
            e => e.Value, 
            (prev, current) => current >= prev);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_WithMultipleGroupsOneInvalid_ReturnsFalse()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new TestEntity { Id = 1, Value = 10m },
            new TestEntity { Id = 1, Value = 20m },
            new TestEntity { Id = 2, Value = 15m },
            new TestEntity { Id = 2, Value = 5m } // Invalid sequence
        };

        // Act
        var result = SequenceValidator.Validate<TestEntity, int, decimal>(
            items, 
            e => e.Id, 
            e => e.Value, 
            (prev, current) => current >= prev);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Validate_WithDefaultValidator_UsesDefaultComparison()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new TestEntity { Id = 1, Value = 10m },
            new TestEntity { Id = 1, Value = 10m },
            new TestEntity { Id = 1, Value = 10m }
        };

        // Act
        var result = SequenceValidator.Validate<TestEntity, int, decimal>(
            items, 
            e => e.Id, 
            e => e.Value);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_WithDefaultValidatorInvalidSequence_ReturnsFalse()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new TestEntity { Id = 1, Value = 30m },
            new TestEntity { Id = 1, Value = 20m },
            new TestEntity { Id = 1, Value = 10m }
        };

        // Act
        var result = SequenceValidator.Validate<TestEntity, int, decimal>(
            items, 
            e => e.Id, 
            e => e.Value);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Validate_WithSummarisationPlan_UsesThresholdValidator()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new TestEntity { Id = 1, Value = 10m },
            new TestEntity { Id = 1, Value = 20m },
            new TestEntity { Id = 1, Value = 30m }
        };

        var plan = new SummarisationPlan<TestEntity>(
            e => e.Value, 
            ThresholdType.PercentChange, 
            100m); // Allow 100% change

        // Act
        var result = SequenceValidator.Validate<TestEntity, int>(
            items, 
            e => e.Id, 
            plan);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_WithSummarisationPlan_NullPlan_ThrowsArgumentNullException()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new TestEntity { Id = 1, Value = 10m }
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            SequenceValidator.Validate<TestEntity, int>(
                items, 
                e => e.Id, 
                null!));
    }

    [Fact]
    public async Task ValidateAgainstLatestAuditAsync_WithValidEntities_ReturnsTrue()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("audit-test")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Value = 10m, Name = "Test", Validated = true }
        };

        // Act
        var result = await SequenceValidator.ValidateAgainstLatestAuditAsync<TestEntity, SaveAudit, int, decimal>(
            entities, 
            context.SaveAudits,
            e => e.Id, 
            audit => int.Parse(audit.EntityId),
            e => e.Value, 
            audit => audit.MetricValue,
            (entityValue, auditValue) => entityValue >= auditValue);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAgainstSaveAuditsAsync_WithValidEntities_ReturnsTrue()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("save-audit-test")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Value = 10m, Name = "Test", Validated = true }
        };

        // Act
        var result = await SequenceValidator.ValidateAgainstSaveAuditsAsync(
            entities, 
            context.SaveAudits,
            e => e.Id, 
            e => e.Value, 
            audit => audit.MetricValue,
            (entityValue, auditValue) => entityValue >= auditValue,
            "TestApp");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAgainstSaveAuditsAsync_WithExistingAudit_ComparesCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("existing-audit-test")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Add an existing audit
        var existingAudit = new SaveAudit
        {
            EntityType = nameof(TestEntity),
            EntityId = "1",
            ApplicationName = "TestApp",
            MetricValue = 5m,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        context.SaveAudits.Add(existingAudit);
        await context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Value = 10m, Name = "Test", Validated = true }
        };

        // Act
        var result = await SequenceValidator.ValidateAgainstSaveAuditsAsync(
            entities, 
            context.SaveAudits,
            e => e.Id, 
            e => e.Value, 
            audit => audit.MetricValue,
            (entityValue, auditValue) => entityValue >= auditValue,
            "TestApp");

        // Assert
        Assert.True(result); // 10 >= 5 should be true
    }

    [Fact]
    public async Task ValidateAgainstSaveAuditsAsync_WithFailingValidation_ReturnsFalse()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("failing-audit-test")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Add an existing audit
        var existingAudit = new SaveAudit
        {
            EntityType = nameof(TestEntity),
            EntityId = "1",
            ApplicationName = "TestApp",
            MetricValue = 20m,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        context.SaveAudits.Add(existingAudit);
        await context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Value = 10m, Name = "Test", Validated = true }
        };

        // Act
        var result = await SequenceValidator.ValidateAgainstSaveAuditsAsync(
            entities, 
            context.SaveAudits,
            e => e.Id, 
            e => e.Value, 
            audit => audit.MetricValue,
            (entityValue, auditValue) => entityValue >= auditValue,
            "TestApp");

        // Assert
        Assert.False(result); // 10 >= 20 should be false
    }
}
