using ExampleLib.Domain;
using ExampleLib.Infrastructure;

namespace ExampleLib.Tests;

/// <summary>
/// Additional tests for ValidationPlan and SummarisationPlan to improve code coverage.
/// </summary>
public class ValidationPlanTests
{
    [Fact]
    public void ValidationPlan_Constructor_WithType_SetsPropertyCorrectly()
    {
        // Arrange
        var propertyType = typeof(string);

        // Act
        var plan = new ValidationPlan(propertyType);

        // Assert
        Assert.Equal(propertyType, plan.PropertyType);
    }

    [Fact]
    public void ValidationPlan_Constructor_WithNullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ValidationPlan(null!));
    }

    [Fact]
    public void ValidationPlan_WithDifferentTypes_CreatesDistinctPlans()
    {
        // Arrange
        var intType = typeof(int);
        var stringType = typeof(string);
        var decimalType = typeof(decimal);

        // Act
        var intPlan = new ValidationPlan(intType);
        var stringPlan = new ValidationPlan(stringType);
        var decimalPlan = new ValidationPlan(decimalType);

        // Assert
        Assert.Equal(intType, intPlan.PropertyType);
        Assert.Equal(stringType, stringPlan.PropertyType);
        Assert.Equal(decimalType, decimalPlan.PropertyType);
        
        Assert.NotEqual(intPlan.PropertyType, stringPlan.PropertyType);
        Assert.NotEqual(stringPlan.PropertyType, decimalPlan.PropertyType);
    }

    public class TestEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public bool Validated { get; set; }
    }

    [Fact]
    public void SummarisationPlan_Constructor_WithAllParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        Func<TestEntity, decimal> metricSelector = e => e.Value;
        var thresholdType = ThresholdType.PercentChange;
        var thresholdValue = 0.15m;

        // Act
        var plan = new SummarisationPlan<TestEntity>(metricSelector, thresholdType, thresholdValue);

        // Assert
        Assert.Equal(metricSelector, plan.MetricSelector);
        Assert.Equal(thresholdType, plan.ThresholdType);
        Assert.Equal(thresholdValue, plan.ThresholdValue);
    }

    [Fact]
    public void SummarisationPlan_Constructor_WithNullMetricSelector_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SummarisationPlan<TestEntity>(null!, ThresholdType.PercentChange, 0.1m));
        Assert.Equal("metricSelector", exception.ParamName);
    }

    [Fact]
    public void SummarisationPlan_MetricSelector_WorksCorrectly()
    {
        // Arrange
        var entity = new TestEntity { Value = 123.45m };
        var plan = new SummarisationPlan<TestEntity>(e => e.Value, ThresholdType.RawDifference, 10m);

        // Act
        var result = plan.MetricSelector(entity);

        // Assert
        Assert.Equal(123.45m, result);
    }

    [Fact]
    public void SummarisationPlan_WithDifferentSelectors_WorksCorrectly()
    {
        // Arrange
        var entity = new TestEntity { Id = 42, Value = 100.5m };
        var idPlan = new SummarisationPlan<TestEntity>(e => e.Id, ThresholdType.RawDifference, 5m);
        var valuePlan = new SummarisationPlan<TestEntity>(e => e.Value, ThresholdType.PercentChange, 0.1m);

        // Act
        var idResult = idPlan.MetricSelector(entity);
        var valueResult = valuePlan.MetricSelector(entity);

        // Assert
        Assert.Equal(42m, idResult);
        Assert.Equal(100.5m, valueResult);
    }

    public class EntityWithComplexProperties : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal BaseAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public bool Validated { get; set; }
        
        public decimal TotalAmount => BaseAmount + TaxAmount;
    }

    [Fact]
    public void SummarisationPlan_WithComplexSelector_WorksCorrectly()
    {
        // Arrange
        var entity = new EntityWithComplexProperties { BaseAmount = 100m, TaxAmount = 15m };
        var plan = new SummarisationPlan<EntityWithComplexProperties>(
            e => e.TotalAmount, 
            ThresholdType.PercentChange, 
            0.2m);

        // Act
        var result = plan.MetricSelector(entity);

        // Assert
        Assert.Equal(115m, result);
    }

    [Fact]
    public void SummarisationPlan_WithDifferentThresholdTypes_SetsCorrectly()
    {
        // Arrange
        Func<TestEntity, decimal> selector = e => e.Value;
        var percentPlan = new SummarisationPlan<TestEntity>(selector, ThresholdType.PercentChange, 0.1m);
        var rawPlan = new SummarisationPlan<TestEntity>(selector, ThresholdType.RawDifference, 25m);

        // Act & Assert
        Assert.Equal(ThresholdType.PercentChange, percentPlan.ThresholdType);
        Assert.Equal(0.1m, percentPlan.ThresholdValue);
        
        Assert.Equal(ThresholdType.RawDifference, rawPlan.ThresholdType);
        Assert.Equal(25m, rawPlan.ThresholdValue);
    }

    [Fact]
    public void SummarisationPlan_WithZeroThresholdValue_WorksCorrectly()
    {
        // Arrange
        Func<TestEntity, decimal> selector = e => e.Value;
        var plan = new SummarisationPlan<TestEntity>(selector, ThresholdType.RawDifference, 0m);

        // Act & Assert
        Assert.Equal(0m, plan.ThresholdValue);
        Assert.Equal(ThresholdType.RawDifference, plan.ThresholdType);
    }

    [Fact]
    public void SummarisationPlan_WithNegativeThresholdValue_WorksCorrectly()
    {
        // Arrange
        Func<TestEntity, decimal> selector = e => e.Value;
        var plan = new SummarisationPlan<TestEntity>(selector, ThresholdType.RawDifference, -5m);

        // Act & Assert
        Assert.Equal(-5m, plan.ThresholdValue);
        Assert.Equal(ThresholdType.RawDifference, plan.ThresholdType);
    }

    [Fact]
    public void SummarisationPlan_WithLargeThresholdValue_WorksCorrectly()
    {
        // Arrange
        Func<TestEntity, decimal> selector = e => e.Value;
        var plan = new SummarisationPlan<TestEntity>(selector, ThresholdType.PercentChange, 999999.99m);

        // Act & Assert
        Assert.Equal(999999.99m, plan.ThresholdValue);
        Assert.Equal(ThresholdType.PercentChange, plan.ThresholdType);
    }

    [Fact]
    public void SummarisationPlan_MultiplePlansWithSameEntity_WorkIndependently()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Value = 100m };
        var plan1 = new SummarisationPlan<TestEntity>(e => e.Value, ThresholdType.PercentChange, 0.1m);
        var plan2 = new SummarisationPlan<TestEntity>(e => e.Id, ThresholdType.RawDifference, 5m);

        // Act
        var result1 = plan1.MetricSelector(entity);
        var result2 = plan2.MetricSelector(entity);

        // Assert
        Assert.Equal(100m, result1);
        Assert.Equal(1m, result2);
        Assert.Equal(ThresholdType.PercentChange, plan1.ThresholdType);
        Assert.Equal(ThresholdType.RawDifference, plan2.ThresholdType);
    }
}