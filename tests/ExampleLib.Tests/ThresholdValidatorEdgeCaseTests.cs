using ExampleLib.Domain;

namespace ExampleLib.Tests;

/// <summary>
/// Tests for ThresholdValidator edge cases to improve coverage.
/// </summary>
public class ThresholdValidatorEdgeCaseTests
{
    [Fact]
    public void IsWithinThreshold_WithZeroCurrentValue_ReturnsCorrectResult()
    {
        // Act
        var result = ThresholdValidator.IsWithinThreshold(0m, 100m, ThresholdType.PercentChange, 10m, false);

        // Assert
        Assert.False(result); // 0 to 100 is more than 10% change
    }

    [Fact]
    public void IsWithinThreshold_WithZeroPreviousValue_ReturnsCorrectResult()
    {
        // Act
        var result = ThresholdValidator.IsWithinThreshold(100m, 0m, ThresholdType.PercentChange, 10m, false);

        // Assert
        Assert.False(result); // 100 to 0 is more than 10% change
    }

    [Fact]
    public void IsWithinThreshold_WithBothZeroValues_ReturnsTrue()
    {
        // Act
        var result = ThresholdValidator.IsWithinThreshold(0m, 0m, ThresholdType.PercentChange, 10m, false);

        // Assert
        Assert.True(result); // No change between two zero values
    }

    [Fact]
    public void IsWithinThreshold_WithNegativeValues_ReturnsCorrectResult()
    {
        // Act
        var result = ThresholdValidator.IsWithinThreshold(-50m, -60m, ThresholdType.RawDifference, 15m, false);

        // Assert
        Assert.True(result); // Absolute difference is 10, which is within 15
    }

    [Fact]
    public void IsWithinThreshold_WithPositiveToNegativeTransition_ReturnsCorrectResult()
    {
        // Act
        var result = ThresholdValidator.IsWithinThreshold(-10m, 10m, ThresholdType.RawDifference, 25m, false);

        // Assert
        Assert.True(result); // Absolute difference is 20, which is within 25
    }

    [Fact]
    public void IsWithinThreshold_WithAbsoluteThresholdExceeded_ReturnsFalse()
    {
        // Act
        var result = ThresholdValidator.IsWithinThreshold(150m, 100m, ThresholdType.RawDifference, 25m, false);

        // Assert
        Assert.False(result); // Absolute difference is 50, which exceeds 25
    }

    [Fact]
    public void IsWithinThreshold_WithPercentageThresholdExceeded_ReturnsFalse()
    {
        // Act
        var result = ThresholdValidator.IsWithinThreshold(150m, 100m, ThresholdType.PercentChange, 25m, false);

        // Assert
        Assert.False(result); // 50% increase exceeds 25% threshold
    }

    [Fact]
    public void IsWithinThreshold_WithPercentageThresholdMet_ReturnsTrue()
    {
        // Act
        var result = ThresholdValidator.IsWithinThreshold(120m, 100m, ThresholdType.PercentChange, 25m, false);

        // Assert
        Assert.True(result); // 20% increase is within 25% threshold
    }

    [Fact]
    public void IsWithinThreshold_WithExactThresholdValue_ReturnsTrue()
    {
        // Act
        var result = ThresholdValidator.IsWithinThreshold(125m, 100m, ThresholdType.PercentChange, 25m, false);

        // Assert
        Assert.True(result); // Exactly 25% increase should be within threshold
    }

    [Fact]
    public void IsWithinThreshold_WithValidatedTrueAndInvalidChange_ReturnsTrue()
    {
        // Act
        var result = ThresholdValidator.IsWithinThreshold(200m, 100m, ThresholdType.PercentChange, 10m, true);

        // Assert
        Assert.True(result); // Even with large change, validated=true should return true
    }

    [Fact]
    public void IsWithinThreshold_WithValidatedFalseAndValidChange_ReturnsTrue()
    {
        // Act
        var result = ThresholdValidator.IsWithinThreshold(105m, 100m, ThresholdType.PercentChange, 10m, false);

        // Assert
        Assert.True(result); // Small change within threshold should return true
    }

    [Fact]
    public void IsWithinThreshold_WithVerySmallPercentageChange_ReturnsTrue()
    {
        // Act
        var result = ThresholdValidator.IsWithinThreshold(100.01m, 100m, ThresholdType.PercentChange, 1m, false);

        // Assert
        Assert.True(result); // 0.01% change is within 1% threshold
    }

    [Fact]
    public void IsWithinThreshold_WithLargeNumbers_ReturnsCorrectResult()
    {
        // Act
        var result = ThresholdValidator.IsWithinThreshold(1000000m, 999000m, ThresholdType.PercentChange, 1m, false);

        // Assert
        Assert.True(result); // ~0.1% change is within 1% threshold
    }

    [Fact]
    public void IsWithinThreshold_WithDecimalPrecision_ReturnsCorrectResult()
    {
        // Act
        var result = ThresholdValidator.IsWithinThreshold(100.123456m, 100.123457m, ThresholdType.RawDifference, 0.000002m, false);

        // Assert
        Assert.True(result); // Very small absolute difference within tiny threshold
    }

    [Fact]
    public void IsWithinThreshold_WithNegativeThreshold_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ThresholdValidator.IsWithinThreshold(100m, 90m, ThresholdType.PercentChange, -10m, false));
    }

    [Fact]
    public void IsWithinThreshold_WithZeroThreshold_ReturnsCorrectResult()
    {
        // Act
        var result = ThresholdValidator.IsWithinThreshold(100m, 100m, ThresholdType.PercentChange, 0m, false);

        // Assert
        Assert.True(result); // No change with zero threshold should return true
    }

    [Fact]
    public void IsWithinThreshold_WithZeroThresholdAndChange_ReturnsFalse()
    {
        // Act
        var result = ThresholdValidator.IsWithinThreshold(101m, 100m, ThresholdType.PercentChange, 0m, false);

        // Assert
        Assert.False(result); // Any change with zero threshold should return false
    }

    [Fact]
    public void IsWithinThreshold_WithVeryLargeThreshold_ReturnsTrue()
    {
        // Act
        var result = ThresholdValidator.IsWithinThreshold(1000m, 100m, ThresholdType.PercentChange, 10000m, false);

        // Assert
        Assert.True(result); // Even 900% change is within 10000% threshold
    }

    [Fact]
    public void IsWithinThreshold_WithPercentageCalculationEdgeCase_ReturnsCorrectResult()
    {
        // Test edge case where previous value is very small
        // Act
        var result = ThresholdValidator.IsWithinThreshold(1m, 0.01m, ThresholdType.PercentChange, 10000m, false);

        // Assert
        Assert.True(result); // Large percentage change should still be within very large threshold
    }
}