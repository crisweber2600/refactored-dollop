namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;

public class ThresholdValidationService : IValidationService
{
    public PipelineResult<bool> IsWithinThreshold(double current, double previous, double maxDelta) =>
        PipelineResult<bool>.Success(Math.Abs(current - previous) <= maxDelta);
}
