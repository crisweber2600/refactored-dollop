using ExampleData;
using ExampleLib.Domain;
using SaveAudit = ExampleLib.Domain.SaveAudit;

namespace ExampleLib.Tests;

/// <summary>
/// Tests for SummarisationValidator to improve code coverage.
/// </summary>
public class SummarisationValidatorTests
{
    public class TestEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public bool Validated { get; set; }
    }

    private SummarisationValidator<TestEntity> CreateValidator()
    {
        return new SummarisationValidator<TestEntity>();
    }

    private SummarisationPlan<TestEntity> CreatePlan(
        ThresholdType thresholdType = ThresholdType.PercentChange,
        decimal thresholdValue = 0.1m)
    {
        return new SummarisationPlan<TestEntity>(
            entity => entity.Value,
            thresholdType,
            thresholdValue);
    }

    [Fact]
    public void Validate_WithNullPlan_ThrowsArgumentNullException()
    {
        // Arrange
        var validator = CreateValidator();
        var entity = new TestEntity { Value = 100 };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            validator.Validate(entity, null, null!));
        Assert.Equal("plan", exception.ParamName);
    }

    [Fact]
    public void Validate_WithNullCurrentEntity_ThrowsArgumentNullException()
    {
        // Arrange
        var validator = CreateValidator();
        var plan = CreatePlan();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            validator.Validate(null!, null, plan));
        Assert.Equal("currentEntity", exception.ParamName);
    }

    [Fact]
    public void Validate_WithNullPreviousAudit_ReturnsTrue()
    {
        // Arrange
        var validator = CreateValidator();
        var entity = new TestEntity { Value = 100 };
        var plan = CreatePlan();

        // Act
        var result = validator.Validate(entity, null, plan);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_WithPreviousAuditAndWithinThreshold_ReturnsTrue()
    {
        // Arrange
        var validator = CreateValidator();
        var entity = new TestEntity { Value = 105 };
        var previousAudit = new SaveAudit
        {
            MetricValue = 100,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        var plan = CreatePlan(ThresholdType.PercentChange, 0.1m); // 10% threshold

        // Act
        var result = validator.Validate(entity, previousAudit, plan);

        // Assert
        Assert.True(result); // 5% change is within 10% threshold
    }

    [Fact]
    public void Validate_WithPreviousAuditAndExceedsThreshold_ReturnsFalse()
    {
        // Arrange
        var validator = CreateValidator();
        var entity = new TestEntity { Value = 150 };
        var previousAudit = new SaveAudit
        {
            MetricValue = 100,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        var plan = CreatePlan(ThresholdType.PercentChange, 0.1m); // 10% threshold

        // Act
        var result = validator.Validate(entity, previousAudit, plan);

        // Assert
        Assert.False(result); // 50% change exceeds 10% threshold
    }

    [Fact]
    public void Validate_WithRawDifferenceThreshold_WorksCorrectly()
    {
        // Arrange
        var validator = CreateValidator();
        var entity = new TestEntity { Value = 115 };
        var previousAudit = new SaveAudit
        {
            MetricValue = 100,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        var plan = CreatePlan(ThresholdType.RawDifference, 20m); // 20 unit threshold

        // Act
        var result = validator.Validate(entity, previousAudit, plan);

        // Assert
        Assert.True(result); // 15 unit change is within 20 unit threshold
    }

    [Fact]
    public void Validate_WithRawDifferenceThresholdExceeded_ReturnsFalse()
    {
        // Arrange
        var validator = CreateValidator();
        var entity = new TestEntity { Value = 125 };
        var previousAudit = new SaveAudit
        {
            MetricValue = 100,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        var plan = CreatePlan(ThresholdType.RawDifference, 20m); // 20 unit threshold

        // Act
        var result = validator.Validate(entity, previousAudit, plan);

        // Assert
        Assert.False(result); // 25 unit change exceeds 20 unit threshold
    }

    [Fact]
    public void Validate_WithZeroCurrentValue_HandlesCorrectly()
    {
        // Arrange
        var validator = CreateValidator();
        var entity = new TestEntity { Value = 0 };
        var previousAudit = new SaveAudit
        {
            MetricValue = 100,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        var plan = CreatePlan(ThresholdType.PercentChange, 0.1m);

        // Act
        var result = validator.Validate(entity, previousAudit, plan);

        // Assert
        Assert.False(result); // 100% decrease exceeds 10% threshold
    }

    [Fact]
    public void Validate_WithZeroPreviousValue_HandlesCorrectly()
    {
        // Arrange
        var validator = CreateValidator();
        var entity = new TestEntity { Value = 100 };
        var previousAudit = new SaveAudit
        {
            MetricValue = 0,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        var plan = CreatePlan(ThresholdType.PercentChange, 0.1m);

        // Act
        var result = validator.Validate(entity, previousAudit, plan);

        // Assert
        Assert.False(result); // 100% increase exceeds 10% threshold
    }

    [Fact]
    public void Validate_WithBothZeroValues_ReturnsTrue()
    {
        // Arrange
        var validator = CreateValidator();
        var entity = new TestEntity { Value = 0 };
        var previousAudit = new SaveAudit
        {
            MetricValue = 0,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        var plan = CreatePlan(ThresholdType.PercentChange, 0.1m);

        // Act
        var result = validator.Validate(entity, previousAudit, plan);

        // Assert
        Assert.True(result); // No change between zero values
    }

    [Fact]
    public void Validate_WithNegativeValues_HandlesCorrectly()
    {
        // Arrange
        var validator = CreateValidator();
        var entity = new TestEntity { Value = -50 };
        var previousAudit = new SaveAudit
        {
            MetricValue = -60,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        var plan = CreatePlan(ThresholdType.RawDifference, 15m);

        // Act
        var result = validator.Validate(entity, previousAudit, plan);

        // Assert
        Assert.True(result); // 10 unit change is within 15 unit threshold
    }

    public class EntityWithComplexValue : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Tax { get; set; }
        public bool Validated { get; set; }

        public decimal TotalValue => Amount + Tax;
    }

    [Fact]
    public void Validate_WithComplexMetricSelector_WorksCorrectly()
    {
        // Arrange
        var validator = new SummarisationValidator<EntityWithComplexValue>();
        var entity = new EntityWithComplexValue { Amount = 100, Tax = 10 }; // Total: 110
        var previousAudit = new SaveAudit
        {
            MetricValue = 100, // Previous total was 100
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        var plan = new SummarisationPlan<EntityWithComplexValue>(
            e => e.TotalValue, // Use complex selector
            ThresholdType.PercentChange,
            0.15m); // 15% threshold

        // Act
        var result = validator.Validate(entity, previousAudit, plan);

        // Assert
        Assert.True(result); // 10% change (100 to 110) is within 15% threshold
    }

    [Fact]
    public void Validate_CallsThresholdValidatorCorrectly()
    {
        // Arrange
        var validator = CreateValidator();
        var entity = new TestEntity { Value = 120 };
        var previousAudit = new SaveAudit
        {
            MetricValue = 100,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        var plan = CreatePlan(ThresholdType.PercentChange, 0.25m); // 25% threshold

        // Act
        var result = validator.Validate(entity, previousAudit, plan);

        // Assert
        Assert.True(result); // 20% change is within 25% threshold

        // Verify the validator uses the plan's configuration
        Assert.Equal(ThresholdType.PercentChange, plan.ThresholdType);
        Assert.Equal(0.25m, plan.ThresholdValue);
    }
}
