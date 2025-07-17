using ExampleLib.Domain;
using ExampleLib.Infrastructure;

namespace ExampleLib.Tests;

/// <summary>
/// Additional tests for ValidationStrategy enum to improve code coverage.
/// </summary>
public class ValidationStrategyTests
{
    [Fact]
    public void ValidationStrategy_HasAllExpectedValues()
    {
        // Act & Assert
        var values = Enum.GetValues<ValidationStrategy>();
        
        Assert.Contains(ValidationStrategy.Sum, values);
        Assert.Contains(ValidationStrategy.Average, values);
        Assert.Contains(ValidationStrategy.Count, values);
        Assert.Contains(ValidationStrategy.Variance, values);
        Assert.Equal(4, values.Length);
    }

    [Fact]
    public void ValidationStrategy_CanBeUsedInSwitch()
    {
        // Arrange
        var strategies = new[]
        {
            ValidationStrategy.Sum,
            ValidationStrategy.Average,
            ValidationStrategy.Count,
            ValidationStrategy.Variance
        };

        // Act & Assert
        foreach (var strategy in strategies)
        {
            var result = strategy switch
            {
                ValidationStrategy.Sum => "Sum",
                ValidationStrategy.Average => "Average",
                ValidationStrategy.Count => "Count",
                ValidationStrategy.Variance => "Variance",
                _ => throw new ArgumentOutOfRangeException()
            };

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
    }

    [Fact]
    public void ValidationStrategy_CanBeCompared()
    {
        // Act & Assert - Test equality with different enum values
        Assert.False(ValidationStrategy.Sum == ValidationStrategy.Average);
        Assert.False(ValidationStrategy.Average == ValidationStrategy.Count);
        
        // Test inequality
        Assert.True(ValidationStrategy.Sum != ValidationStrategy.Average);
        Assert.True(ValidationStrategy.Count != ValidationStrategy.Variance);
        
        // Test that same values are equal using variables
        var strategy1 = ValidationStrategy.Sum;
        var strategy2 = ValidationStrategy.Sum;
        Assert.True(strategy1 == strategy2);
    }

    [Fact]
    public void ValidationStrategy_CanBeUsedInCollections()
    {
        // Arrange
        var strategies = new HashSet<ValidationStrategy>
        {
            ValidationStrategy.Sum,
            ValidationStrategy.Average,
            ValidationStrategy.Count,
            ValidationStrategy.Variance
        };

        // Act & Assert
        Assert.Contains(ValidationStrategy.Sum, strategies);
        Assert.Contains(ValidationStrategy.Average, strategies);
        Assert.Contains(ValidationStrategy.Count, strategies);
        Assert.Contains(ValidationStrategy.Variance, strategies);
        Assert.Equal(4, strategies.Count);
    }

    [Fact]
    public void ValidationStrategy_HasCorrectStringRepresentation()
    {
        // Act & Assert
        Assert.Equal("Sum", ValidationStrategy.Sum.ToString());
        Assert.Equal("Average", ValidationStrategy.Average.ToString());
        Assert.Equal("Count", ValidationStrategy.Count.ToString());
        Assert.Equal("Variance", ValidationStrategy.Variance.ToString());
    }
}

/// <summary>
/// Additional tests for ThresholdType enum to improve code coverage.
/// </summary>
public class ThresholdTypeTests
{
    [Fact]
    public void ThresholdType_HasAllExpectedValues()
    {
        // Act & Assert
        var values = Enum.GetValues<ThresholdType>();
        
        Assert.Contains(ThresholdType.PercentChange, values);
        Assert.Contains(ThresholdType.RawDifference, values);
        Assert.Equal(2, values.Length);
    }

    [Fact]
    public void ThresholdType_CanBeUsedInSwitch()
    {
        // Arrange
        var types = new[]
        {
            ThresholdType.PercentChange,
            ThresholdType.RawDifference
        };

        // Act & Assert
        foreach (var type in types)
        {
            var result = type switch
            {
                ThresholdType.PercentChange => "PercentChange",
                ThresholdType.RawDifference => "RawDifference",
                _ => throw new ArgumentOutOfRangeException()
            };

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
    }

    [Fact]
    public void ThresholdType_CanBeCompared()
    {
        // Act & Assert - Test equality with different enum values
        Assert.False(ThresholdType.PercentChange == ThresholdType.RawDifference);
        Assert.False(ThresholdType.RawDifference == ThresholdType.PercentChange);
        
        // Test inequality
        Assert.True(ThresholdType.PercentChange != ThresholdType.RawDifference);
        Assert.True(ThresholdType.RawDifference != ThresholdType.PercentChange);
        
        // Test that same values are equal using variables
        var type1 = ThresholdType.PercentChange;
        var type2 = ThresholdType.PercentChange;
        Assert.True(type1 == type2);
    }

    [Fact]
    public void ThresholdType_CanBeUsedInCollections()
    {
        // Arrange
        var types = new HashSet<ThresholdType>
        {
            ThresholdType.PercentChange,
            ThresholdType.RawDifference
        };

        // Act & Assert
        Assert.Contains(ThresholdType.PercentChange, types);
        Assert.Contains(ThresholdType.RawDifference, types);
        Assert.Equal(2, types.Count);
    }

    [Fact]
    public void ThresholdType_HasCorrectStringRepresentation()
    {
        // Act & Assert
        Assert.Equal("PercentChange", ThresholdType.PercentChange.ToString());
        Assert.Equal("RawDifference", ThresholdType.RawDifference.ToString());
    }
}