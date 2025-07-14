using ExampleData;
using ExampleLib.Domain;
using SaveAudit = ExampleLib.Domain.SaveAudit;

namespace ExampleLib.Tests;

public class SummarisationValidatorTests
{
    private readonly ISummarisationValidator<YourEntity> _validator = new SummarisationValidator<YourEntity>();

    [Fact]
    public void NoPreviousAudit_ReturnsTrue()
    {
        var plan = new SummarisationPlan<YourEntity>(e => e.Id, ThresholdType.RawDifference, 5);
        var entity = new YourEntity { Id = 10 };

        var result = _validator.Validate(entity, null, plan);

        Assert.True(result);
    }

    [Fact]
    public void RawDifferenceWithinThreshold_ReturnsTrue()
    {
        var plan = new SummarisationPlan<YourEntity>(e => e.Id, ThresholdType.RawDifference, 5);
        var entity = new YourEntity { Id = 12 };
        var previous = new SaveAudit { MetricValue = 10 };

        var result = _validator.Validate(entity, previous, plan);

        Assert.True(result);
    }

    [Fact]
    public void PercentChangeExceedsThreshold_ReturnsFalse()
    {
        var plan = new SummarisationPlan<YourEntity>(e => e.Id, ThresholdType.PercentChange, 0.1m);
        var entity = new YourEntity { Id = 12 };
        var previous = new SaveAudit { MetricValue = 10 };

        var result = _validator.Validate(entity, previous, plan);

        Assert.False(result);
    }

    [Fact]
    public void UnknownThresholdType_ReturnsTrue()
    {
        var plan = new SummarisationPlan<YourEntity>(e => e.Id, (ThresholdType)999, 1m);
        var entity = new YourEntity { Id = 1 };
        var previous = new SaveAudit { MetricValue = 1 };

        var result = _validator.Validate(entity, previous, plan);

        Assert.True(result);
    }
}
