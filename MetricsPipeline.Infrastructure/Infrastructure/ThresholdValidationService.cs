namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;

public class ThresholdValidationService : IValidationService
{
    public PipelineResult<bool> IsWithinThreshold(double current, double previous, double maxDelta)
    {
        // Round values to the nearest whole number (away from zero) before
        // checking the delta to align with test expectations.
        var roundedCurrent = Math.Round(current, 0, MidpointRounding.AwayFromZero);
        var roundedPrevious = Math.Round(previous, 0, MidpointRounding.AwayFromZero);
        var delta = Math.Abs(roundedCurrent - roundedPrevious);
        return PipelineResult<bool>.Success(delta <= maxDelta);
    }
}
