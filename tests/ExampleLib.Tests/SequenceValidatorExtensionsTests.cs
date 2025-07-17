using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExampleLib.Tests;

/// <summary>
/// Comprehensive unit tests for SequenceValidatorExtensions methods.
/// Tests all extension methods that integrate SequenceValidator with IEntityIdProvider.
/// </summary>
public class SequenceValidatorExtensionsTests
{
    /// <summary>
    /// Test entity used for validation testing
    /// </summary>
    private class TestEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public bool Validated { get; set; }
    }

    /// <summary>
    /// Complex test entity with more properties for comprehensive testing
    /// </summary>
    private class ComplexTestEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public bool IsActive { get; set; }
        public bool Validated { get; set; }
    }

    #region ValidateAgainstAuditsWithProviderAsync Tests

    [Fact]
    public async Task ValidateAgainstAuditsWithProviderAsync_ReturnsTrue_WhenAllEntitiesPassValidation()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-audits-success")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Seed audit data
        var existingAudit = new SaveAudit
        {
            EntityType = nameof(TestEntity),
            EntityId = "TestEntity1",
            ApplicationName = "TestApp",
            MetricValue = 100.0m,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        context.SaveAudits.Add(existingAudit);
        await context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "TestEntity1", Value = 105.0m, Validated = true }
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        // Act
        var result = await SequenceValidatorExtensions.ValidateAgainstAuditsWithProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            entity => entity.Value,
            audit => audit.MetricValue,
            (newValue, auditValue) => Math.Abs(newValue - auditValue) <= 10m,
            "TestApp",
            CancellationToken.None);

        // Assert
        Assert.True(result);
        mockEntityIdProvider.Verify(p => p.GetEntityId(It.IsAny<TestEntity>()), Times.Once);
    }

    [Fact]
    public async Task ValidateAgainstAuditsWithProviderAsync_ReturnsFalse_WhenValidationFails()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-audits-fail")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Seed audit data
        var existingAudit = new SaveAudit
        {
            EntityType = nameof(TestEntity),
            EntityId = "TestEntity1",
            ApplicationName = "TestApp",
            MetricValue = 100.0m,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        context.SaveAudits.Add(existingAudit);
        await context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "TestEntity1", Value = 150.0m, Validated = true } // Exceeds threshold of 10
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        // Act
        var result = await SequenceValidatorExtensions.ValidateAgainstAuditsWithProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            entity => entity.Value,
            audit => audit.MetricValue,
            (newValue, auditValue) => Math.Abs(newValue - auditValue) <= 10m,
            "TestApp",
            CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAgainstAuditsWithProviderAsync_ReturnsTrue_WhenNoAuditExists()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-audits-no-audit")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "NewEntity", Value = 100.0m, Validated = true }
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        // Act - No audit data exists, should pass validation
        var result = await SequenceValidatorExtensions.ValidateAgainstAuditsWithProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            entity => entity.Value,
            audit => audit.MetricValue,
            (newValue, auditValue) => Math.Abs(newValue - auditValue) <= 10m,
            "TestApp",
            CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAgainstAuditsWithProviderAsync_ReturnsTrue_WhenEntitiesListIsEmpty()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-audits-empty")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        var entities = new List<TestEntity>();

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();

        // Act
        var result = await SequenceValidatorExtensions.ValidateAgainstAuditsWithProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            entity => entity.Value,
            audit => audit.MetricValue,
            (newValue, auditValue) => true,
            "TestApp",
            CancellationToken.None);

        // Assert
        Assert.True(result);
        mockEntityIdProvider.Verify(p => p.GetEntityId(It.IsAny<TestEntity>()), Times.Never);
    }

    [Fact]
    public async Task ValidateAgainstAuditsWithProviderAsync_HandlesMultipleEntitiesCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-audits-multiple")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Seed audit data for multiple entities
        var audits = new List<SaveAudit>
        {
            new()
            {
                EntityType = nameof(TestEntity),
                EntityId = "Entity1",
                ApplicationName = "TestApp",
                MetricValue = 100.0m,
                Validated = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-2)
            },
            new()
            {
                EntityType = nameof(TestEntity),
                EntityId = "Entity2",
                ApplicationName = "TestApp",
                MetricValue = 200.0m,
                Validated = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
            }
        };
        context.SaveAudits.AddRange(audits);
        await context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "Entity1", Value = 105.0m, Validated = true }, // Should pass (105 vs 100, diff = 5)
            new() { Id = 2, Name = "Entity2", Value = 195.0m, Validated = true }  // Should pass (195 vs 200, diff = 5)
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        // Act
        var result = await SequenceValidatorExtensions.ValidateAgainstAuditsWithProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            entity => entity.Value,
            audit => audit.MetricValue,
            (newValue, auditValue) => Math.Abs(newValue - auditValue) <= 10m,
            "TestApp",
            CancellationToken.None);

        // Assert
        Assert.True(result);
        mockEntityIdProvider.Verify(p => p.GetEntityId(It.IsAny<TestEntity>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ValidateAgainstAuditsWithProviderAsync_ReturnsFalse_WhenOneEntityFailsValidation()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-audits-one-fails")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Seed audit data
        var audits = new List<SaveAudit>
        {
            new()
            {
                EntityType = nameof(TestEntity),
                EntityId = "Entity1",
                ApplicationName = "TestApp",
                MetricValue = 100.0m,
                Validated = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
            },
            new()
            {
                EntityType = nameof(TestEntity),
                EntityId = "Entity2",
                ApplicationName = "TestApp",
                MetricValue = 200.0m,
                Validated = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
            }
        };
        context.SaveAudits.AddRange(audits);
        await context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "Entity1", Value = 105.0m, Validated = true }, // Should pass (diff = 5)
            new() { Id = 2, Name = "Entity2", Value = 250.0m, Validated = true }  // Should fail (diff = 50 > 10)
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        // Act
        var result = await SequenceValidatorExtensions.ValidateAgainstAuditsWithProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            entity => entity.Value,
            audit => audit.MetricValue,
            (newValue, auditValue) => Math.Abs(newValue - auditValue) <= 10m,
            "TestApp",
            CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAgainstAuditsWithProviderAsync_UsesLatestAuditWhenMultipleExist()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-audits-latest")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Seed multiple audit records for the same entity
        var audits = new List<SaveAudit>
        {
            new()
            {
                EntityType = nameof(TestEntity),
                EntityId = "Entity1",
                ApplicationName = "TestApp",
                MetricValue = 50.0m,
                Validated = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-10) // Older audit
            },
            new()
            {
                EntityType = nameof(TestEntity),
                EntityId = "Entity1",
                ApplicationName = "TestApp",
                MetricValue = 100.0m,
                Validated = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1) // Latest audit
            }
        };
        context.SaveAudits.AddRange(audits);
        await context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "Entity1", Value = 105.0m, Validated = true } // Should use latest audit value (100)
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        // Act
        var result = await SequenceValidatorExtensions.ValidateAgainstAuditsWithProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            entity => entity.Value,
            audit => audit.MetricValue,
            (newValue, auditValue) => Math.Abs(newValue - auditValue) <= 10m,
            "TestApp",
            CancellationToken.None);

        // Assert
        Assert.True(result); // 105 vs 100 (latest) = 5, which is <= 10
    }

    [Fact]
    public async Task ValidateAgainstAuditsWithProviderAsync_WithCancellationToken_HandlesRequestCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-audits-cancellation")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "Entity1", Value = 100.0m, Validated = true }
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        using var cts = new CancellationTokenSource();

        // Act
        var result = await SequenceValidatorExtensions.ValidateAgainstAuditsWithProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            entity => entity.Value,
            audit => audit.MetricValue,
            (newValue, auditValue) => true,
            "TestApp",
            cts.Token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAgainstAuditsWithProviderAsync_WithComplexEntity_WorksCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-audits-complex")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Seed audit data
        var existingAudit = new SaveAudit
        {
            EntityType = nameof(ComplexTestEntity),
            EntityId = "PROD001",
            ApplicationName = "TestApp",
            MetricValue = 1500.0m, // Price * Quantity = 15.00 * 100
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        context.SaveAudits.Add(existingAudit);
        await context.SaveChangesAsync();

        var entities = new List<ComplexTestEntity>
        {
            new() 
            { 
                Id = 1, 
                Code = "PROD001", 
                Name = "Product 1", 
                Price = 16.00m, 
                Quantity = 95, 
                IsActive = true, 
                Validated = true 
            }
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<ComplexTestEntity>()))
            .Returns<ComplexTestEntity>(e => e.Code);

        // Act - Using complex value selector (Price * Quantity)
        var result = await SequenceValidatorExtensions.ValidateAgainstAuditsWithProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            entity => entity.Price * entity.Quantity,
            audit => audit.MetricValue,
            (newValue, auditValue) => Math.Abs(newValue - auditValue) <= 100m,
            "TestApp",
            CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAgainstAuditsWithProviderAsync_WithDifferentValueTypes_WorksCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-audits-diff-types")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Seed audit data
        var existingAudit = new SaveAudit
        {
            EntityType = nameof(ComplexTestEntity),
            EntityId = "PROD001",
            ApplicationName = "TestApp",
            MetricValue = 100.0m,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        context.SaveAudits.Add(existingAudit);
        await context.SaveChangesAsync();

        var entities = new List<ComplexTestEntity>
        {
            new() 
            { 
                Id = 1, 
                Code = "PROD001", 
                Name = "Product 1", 
                Price = 15.00m, 
                Quantity = 95, 
                IsActive = true, 
                Validated = true 
            }
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<ComplexTestEntity>()))
            .Returns<ComplexTestEntity>(e => e.Code);

        // Act - Using int value from entity vs decimal from audit
        var result = await SequenceValidatorExtensions.ValidateAgainstAuditsWithProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            entity => entity.Quantity, // int type
            audit => (int)audit.MetricValue, // decimal to int
            (newValue, auditValue) => Math.Abs(newValue - auditValue) <= 10,
            "TestApp",
            CancellationToken.None);

        // Assert
        Assert.True(result); // 95 vs 100 = 5, within threshold of 10
    }

    [Fact]
    public async Task ValidateAgainstAuditsWithProviderAsync_WithNullEntityIdProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-audits-null-provider")
            .Options;

        using var context = new TheNannyDbContext(options);
        var entities = new List<TestEntity> { new() { Id = 1, Name = "Test", Value = 100, Validated = true } };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await SequenceValidatorExtensions.ValidateAgainstAuditsWithProviderAsync(
                entities,
                context.SaveAudits,
                null!, // Null provider
                entity => entity.Value,
                audit => audit.MetricValue,
                (newValue, auditValue) => true,
                "TestApp",
                CancellationToken.None));
    }

    #endregion

    #region ValidateWithPlanAndProviderAsync Tests

    [Fact]
    public async Task ValidateWithPlanAndProviderAsync_ReturnsTrue_WhenAllEntitiesPassValidation()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-plan-success")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Seed audit data
        var existingAudit = new SaveAudit
        {
            EntityType = nameof(TestEntity),
            EntityId = "TestEntity1",
            ApplicationName = "TestApp",
            MetricValue = 100.0m,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        context.SaveAudits.Add(existingAudit);
        await context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "TestEntity1", Value = 105.0m, Validated = true }
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        var plan = new ValidationPlan(typeof(TestEntity), threshold: 10.0); // Allow difference of 10

        // Act
        var result = await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            plan,
            entity => entity.Value,
            "TestApp",
            CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateWithPlanAndProviderAsync_ReturnsFalse_WhenValidationFails()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-plan-fail")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Seed audit data
        var existingAudit = new SaveAudit
        {
            EntityType = nameof(TestEntity),
            EntityId = "TestEntity1",
            ApplicationName = "TestApp",
            MetricValue = 100.0m,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        context.SaveAudits.Add(existingAudit);
        await context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "TestEntity1", Value = 150.0m, Validated = true } // Exceeds threshold of 5
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        var plan = new ValidationPlan(typeof(TestEntity), threshold: 5.0); // Strict threshold

        // Act
        var result = await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            plan,
            entity => entity.Value,
            "TestApp",
            CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateWithPlanAndProviderAsync_ReturnsTrue_WhenNoAuditExists()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-plan-no-audit")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "NewEntity", Value = 100.0m, Validated = true }
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        var plan = new ValidationPlan(typeof(TestEntity), threshold: 5.0);

        // Act - No audit data exists, should pass validation
        var result = await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            plan,
            entity => entity.Value,
            "TestApp",
            CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateWithPlanAndProviderAsync_WithCancellationToken_HandlesRequestCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-plan-cancellation")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "Entity1", Value = 100.0m, Validated = true }
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        var plan = new ValidationPlan(typeof(TestEntity), threshold: 10.0);

        using var cts = new CancellationTokenSource();

        // Act
        var result = await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            plan,
            entity => entity.Value,
            "TestApp",
            cts.Token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateWithPlanAndProviderAsync_ValidatesWithinPlanThreshold()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-plan-threshold")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Seed audit data
        var existingAudit = new SaveAudit
        {
            EntityType = nameof(TestEntity),
            EntityId = "TestEntity1",
            ApplicationName = "TestApp",
            MetricValue = 100.0m,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        context.SaveAudits.Add(existingAudit);
        await context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "TestEntity1", Value = 120.0m, Validated = true } // Exactly at threshold (20 difference)
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        var plan = new ValidationPlan(typeof(TestEntity), threshold: 20.0); // Exactly at threshold

        // Act
        var result = await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            plan,
            entity => entity.Value,
            "TestApp",
            CancellationToken.None);

        // Assert
        Assert.True(result); // Should pass as 20 <= 20.0
    }

    [Fact]
    public async Task ValidateWithPlanAndProviderAsync_HandlesMultipleEntitiesWithMixedResults()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-plan-mixed")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Seed audit data for multiple entities
        var audits = new List<SaveAudit>
        {
            new()
            {
                EntityType = nameof(TestEntity),
                EntityId = "Entity1",
                ApplicationName = "TestApp",
                MetricValue = 100.0m,
                Validated = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
            },
            new()
            {
                EntityType = nameof(TestEntity),
                EntityId = "Entity2",
                ApplicationName = "TestApp",
                MetricValue = 200.0m,
                Validated = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
            }
        };
        context.SaveAudits.AddRange(audits);
        await context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "Entity1", Value = 105.0m, Validated = true }, // Should pass (diff = 5)
            new() { Id = 2, Name = "Entity2", Value = 250.0m, Validated = true }  // Should fail (diff = 50 > 10)
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        var plan = new ValidationPlan(typeof(TestEntity), threshold: 10.0);

        // Act
        var result = await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            plan,
            entity => entity.Value,
            "TestApp",
            CancellationToken.None);

        // Assert
        Assert.False(result); // Should fail because one entity exceeds threshold
    }

    [Fact]
    public async Task ValidateWithPlanAndProviderAsync_DelegatesToValidateAgainstAuditsWithProviderAsync()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-plan-delegation")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Seed audit data
        var existingAudit = new SaveAudit
        {
            EntityType = nameof(TestEntity),
            EntityId = "TestEntity1",
            ApplicationName = "TestApp",
            MetricValue = 100.0m,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        context.SaveAudits.Add(existingAudit);
        await context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "TestEntity1", Value = 100.0m, Validated = true } // Exactly same value (diff = 0)
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        var plan = new ValidationPlan(typeof(TestEntity), threshold: 5.0);

        // Act
        var result = await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            plan,
            entity => entity.Value,
            "TestApp",
            CancellationToken.None);

        // Assert
        Assert.True(result); // Should pass as difference is 0 <= 5.0
        
        // Verify that the EntityIdProvider was called with the entity
        mockEntityIdProvider.Verify(p => p.GetEntityId(It.IsAny<TestEntity>()), Times.Once);
    }

    [Fact]
    public async Task ValidateWithPlanAndProviderAsync_WithZeroThreshold_WorksCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-plan-zero-threshold")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Seed audit data
        var existingAudit = new SaveAudit
        {
            EntityType = nameof(TestEntity),
            EntityId = "TestEntity1",
            ApplicationName = "TestApp",
            MetricValue = 100.0m,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        context.SaveAudits.Add(existingAudit);
        await context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "TestEntity1", Value = 100.0m, Validated = true } // Exactly same value
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        var plan = new ValidationPlan(typeof(TestEntity), threshold: 0.0); // Zero threshold - must be exact

        // Act
        var result = await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            plan,
            entity => entity.Value,
            "TestApp",
            CancellationToken.None);

        // Assert
        Assert.True(result); // Should pass as difference is exactly 0
    }

    [Fact]
    public async Task ValidateWithPlanAndProviderAsync_WithZeroThreshold_ReturnsFalse_WhenNotExact()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-plan-zero-threshold-fail")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Seed audit data
        var existingAudit = new SaveAudit
        {
            EntityType = nameof(TestEntity),
            EntityId = "TestEntity1",
            ApplicationName = "TestApp",
            MetricValue = 100.0m,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        context.SaveAudits.Add(existingAudit);
        await context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "TestEntity1", Value = 100.1m, Validated = true } // Slightly different
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        var plan = new ValidationPlan(typeof(TestEntity), threshold: 0.0); // Zero threshold - must be exact

        // Act
        var result = await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            plan,
            entity => entity.Value,
            "TestApp",
            CancellationToken.None);

        // Assert
        Assert.False(result); // Should fail as difference is 0.1 > 0
    }

    [Fact]
    public async Task ValidateWithPlanAndProviderAsync_WithDifferentValidationStrategies_WorksCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-plan-strategies")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "Entity1", Value = 100.0m, Validated = true }
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        // Test different ValidationStrategy values
        var strategies = new[] 
        { 
            ValidationStrategy.Count, 
            ValidationStrategy.Sum, 
            ValidationStrategy.Average, 
            ValidationStrategy.Variance 
        };

        foreach (var strategy in strategies)
        {
            var plan = new ValidationPlan(typeof(TestEntity), threshold: 10.0, strategy);

            // Act
            var result = await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
                entities,
                context.SaveAudits,
                mockEntityIdProvider.Object,
                plan,
                entity => entity.Value,
                "TestApp",
                CancellationToken.None);

            // Assert - Since there's no audit, should always pass regardless of strategy
            Assert.True(result, $"Validation should pass for strategy {strategy}");
        }
    }

    [Fact]
    public async Task ValidateWithPlanAndProviderAsync_WithNullArguments_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-plan-null-args")
            .Options;

        using var context = new TheNannyDbContext(options);
        var entities = new List<TestEntity> { new() { Id = 1, Name = "Test", Value = 100, Validated = true } };
        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        var plan = new ValidationPlan(typeof(TestEntity), threshold: 10.0);

        // Act & Assert - Test null entityIdProvider
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
                entities,
                context.SaveAudits,
                null!, // Null provider
                plan,
                entity => entity.Value,
                "TestApp",
                CancellationToken.None));

        // Act & Assert - Test null plan
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
                entities,
                context.SaveAudits,
                mockEntityIdProvider.Object,
                null!, // Null plan
                entity => entity.Value,
                "TestApp",
                CancellationToken.None));
    }

    [Fact]
    public async Task ValidateWithPlanAndProviderAsync_WithComplexValueSelector_WorksCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-plan-complex-selector")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Seed audit data
        var existingAudit = new SaveAudit
        {
            EntityType = nameof(ComplexTestEntity),
            EntityId = "PROD001",
            ApplicationName = "TestApp",
            MetricValue = 1500.0m, // Total value from previous calculation
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        context.SaveAudits.Add(existingAudit);
        await context.SaveChangesAsync();

        var entities = new List<ComplexTestEntity>
        {
            new() 
            { 
                Id = 1, 
                Code = "PROD001", 
                Name = "Product 1", 
                Price = 15.5m, 
                Quantity = 97, 
                IsActive = true, 
                Validated = true 
            }
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<ComplexTestEntity>()))
            .Returns<ComplexTestEntity>(e => e.Code);

        var plan = new ValidationPlan(typeof(ComplexTestEntity), threshold: 100.0);

        // Act - Using complex value selector (Price * Quantity)
        var result = await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            plan,
            entity => entity.Price * entity.Quantity, // 15.5 * 97 = 1503.5
            "TestApp",
            CancellationToken.None);

        // Assert - 1503.5 vs 1500 = 3.5, which is <= 100
        Assert.True(result);
    }

    #endregion

    #region Integration and Edge Case Tests

    [Fact]
    public async Task ValidateAgainstAuditsWithProviderAsync_WithLargeDataset_PerformsEfficiently()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-audits-large-dataset")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Seed audit data for 1000 entities
        var audits = new List<SaveAudit>();
        for (int i = 1; i <= 1000; i++)
        {
            audits.Add(new SaveAudit
            {
                EntityType = nameof(TestEntity),
                EntityId = $"Entity{i}",
                ApplicationName = "TestApp",
                MetricValue = 100.0m + i,
                Validated = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }
        context.SaveAudits.AddRange(audits);
        await context.SaveChangesAsync();

        // Create entities to validate
        var entities = new List<TestEntity>();
        for (int i = 1; i <= 1000; i++)
        {
            entities.Add(new TestEntity
            {
                Id = i,
                Name = $"Entity{i}",
                Value = 105.0m + i, // Within threshold
                Validated = true
            });
        }

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await SequenceValidatorExtensions.ValidateAgainstAuditsWithProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            entity => entity.Value,
            audit => audit.MetricValue,
            (newValue, auditValue) => Math.Abs(newValue - auditValue) <= 10m,
            "TestApp",
            CancellationToken.None);

        stopwatch.Stop();

        // Assert
        Assert.True(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Performance test failed: took {stopwatch.ElapsedMilliseconds}ms");
        mockEntityIdProvider.Verify(p => p.GetEntityId(It.IsAny<TestEntity>()), Times.Exactly(1000));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(10.0)]
    [InlineData(100.0)]
    [InlineData(1000.0)]
    public async Task ValidateWithPlanAndProviderAsync_WithVariousThresholds_WorksCorrectly(double threshold)
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase($"validate-plan-threshold-{threshold}")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Seed audit data
        var existingAudit = new SaveAudit
        {
            EntityType = nameof(TestEntity),
            EntityId = "TestEntity1",
            ApplicationName = "TestApp",
            MetricValue = 100.0m,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        context.SaveAudits.Add(existingAudit);
        await context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "TestEntity1", Value = 105.0m, Validated = true } // Difference of 5
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        var plan = new ValidationPlan(typeof(TestEntity), threshold: threshold);

        // Act
        var result = await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            plan,
            entity => entity.Value,
            "TestApp",
            CancellationToken.None);

        // Assert
        bool expectedResult = 5.0 <= threshold;
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ValidateAgainstAuditsWithProviderAsync_WithCustomValidationFunction_WorksCorrectly()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TheNannyDbContext>()
            .UseInMemoryDatabase("validate-audits-custom-function")
            .Options;

        using var context = new TheNannyDbContext(options);
        context.Database.EnsureCreated();

        // Seed audit data
        var existingAudit = new SaveAudit
        {
            EntityType = nameof(TestEntity),
            EntityId = "TestEntity1",
            ApplicationName = "TestApp",
            MetricValue = 100.0m,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        context.SaveAudits.Add(existingAudit);
        await context.SaveChangesAsync();

        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "TestEntity1", Value = 80.0m, Validated = true } // 20% decrease
        };

        var mockEntityIdProvider = new Mock<IEntityIdProvider>();
        mockEntityIdProvider.Setup(p => p.GetEntityId(It.IsAny<TestEntity>()))
            .Returns<TestEntity>(e => e.Name);

        // Act - Custom validation function for percentage change
        var result = await SequenceValidatorExtensions.ValidateAgainstAuditsWithProviderAsync(
            entities,
            context.SaveAudits,
            mockEntityIdProvider.Object,
            entity => entity.Value,
            audit => audit.MetricValue,
            (newValue, auditValue) => 
            {
                var percentChange = Math.Abs((newValue - auditValue) / auditValue);
                return percentChange <= 0.25m; // Allow up to 25% change
            },
            "TestApp",
            CancellationToken.None);

        // Assert
        Assert.True(result); // 20% change should be within 25% threshold
    }

    #endregion
}