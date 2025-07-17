using ExampleLib.Domain;

namespace ExampleLib.Tests;

/// <summary>
/// Tests for ThresholdValidator to improve code coverage.
/// </summary>
public class ThresholdValidatorTests
{
    [Fact]
    public void ThrowsOnUnsupportedType_WhenRequested()
    {
        // Arrange
        var unsupportedType = (ThresholdType)999; // Non-existent enum value

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
            ThresholdValidator.IsWithinThreshold(100, 90, unsupportedType, 10, throwOnUnsupported: true));
        
        Assert.Contains("Unsupported ThresholdType", exception.Message);
    }

    [Fact]
    public void IsWithinThreshold_WithValidatedFlag_ReturnsTrue()
    {
        // Arrange
        var current = 100m;
        var previous = 50m;
        var threshold = 10m;

        // Act
        var result = ThresholdValidator.IsWithinThreshold(current, previous, ThresholdType.RawDifference, threshold, validated: true);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsWithinThreshold_WithNegativeThreshold_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            ThresholdValidator.IsWithinThreshold(100, 90, ThresholdType.RawDifference, -5));
        
        Assert.Contains("Threshold value cannot be negative", exception.Message);
        Assert.Equal("threshold", exception.ParamName);
    }

    [Fact]
    public void IsWithinThreshold_RawDifference_WithinThreshold_ReturnsTrue()
    {
        // Arrange
        var current = 105m;
        var previous = 100m;
        var threshold = 10m;

        // Act
        var result = ThresholdValidator.IsWithinThreshold(current, previous, ThresholdType.RawDifference, threshold);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsWithinThreshold_RawDifference_ExceedsThreshold_ReturnsFalse()
    {
        // Arrange
        var current = 120m;
        var previous = 100m;
        var threshold = 10m;

        // Act
        var result = ThresholdValidator.IsWithinThreshold(current, previous, ThresholdType.RawDifference, threshold);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsWithinThreshold_PercentChange_WithinThreshold_ReturnsTrue()
    {
        // Arrange
        var current = 105m;
        var previous = 100m;
        var threshold = 10m; // 10% threshold

        // Act
        var result = ThresholdValidator.IsWithinThreshold(current, previous, ThresholdType.PercentChange, threshold);

        // Assert
        Assert.True(result); // 5% change is within 10% threshold
    }

    [Fact]
    public void IsWithinThreshold_PercentChange_ExceedsThreshold_ReturnsFalse()
    {
        // Arrange
        var current = 120m;
        var previous = 100m;
        var threshold = 10m; // 10% threshold

        // Act
        var result = ThresholdValidator.IsWithinThreshold(current, previous, ThresholdType.PercentChange, threshold);

        // Assert
        Assert.False(result); // 20% change exceeds 10% threshold
    }

    [Fact]
    public void IsWithinThreshold_PercentChange_WithZeroPrevious_CurrentZero_ReturnsTrue()
    {
        // Arrange
        var current = 0m;
        var previous = 0m;
        var threshold = 10m;

        // Act
        var result = ThresholdValidator.IsWithinThreshold(current, previous, ThresholdType.PercentChange, threshold);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsWithinThreshold_PercentChange_WithZeroPrevious_CurrentNonZero_ReturnsFalse()
    {
        // Arrange
        var current = 100m;
        var previous = 0m;
        var threshold = 10m;

        // Act
        var result = ThresholdValidator.IsWithinThreshold(current, previous, ThresholdType.PercentChange, threshold);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsWithinThreshold_UnsupportedType_ThrowOnUnsupportedFalse_ReturnsTrue()
    {
        // Arrange
        var unsupportedType = (ThresholdType)999;

        // Act
        var result = ThresholdValidator.IsWithinThreshold(100, 90, unsupportedType, 10, throwOnUnsupported: false);

        // Assert
        Assert.True(result);
    }
}