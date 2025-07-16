using ExampleLib.Domain;

namespace ExampleLib.Tests;

public class ThresholdValidatorTests
{
    [Fact]
    public void ThrowsOnUnsupportedType_WhenRequested()
    {
        Assert.Throws<NotSupportedException>(() =>
            ThresholdValidator.IsWithinThreshold(10, 5, (ThresholdType)123, 1m, true));
    }
}

