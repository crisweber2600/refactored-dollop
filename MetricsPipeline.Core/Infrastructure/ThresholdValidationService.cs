namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;

/// <summary>
/// Validation service that compares current and previous summaries using a threshold.
/// </summary>
public class ThresholdValidationService : IValidationService
{
    private readonly ISummarizationService _summarizer;

    public ThresholdValidationService(ISummarizationService summarizer)
    {
        _summarizer = summarizer;
    }

    /// <inheritdoc />
    public PipelineResult<bool> IsWithinThreshold(double current, double previous, double maxDelta)
    {
        var roundedCurrent = Math.Round(current, 0, MidpointRounding.AwayFromZero);
        var roundedPrevious = Math.Round(previous, 0, MidpointRounding.AwayFromZero);
        var delta = Math.Abs(roundedCurrent - roundedPrevious);
        return PipelineResult<bool>.Success(delta <= maxDelta);
    }

    /// <inheritdoc />
    public PipelineResult<bool> IsWithinThreshold<T>(IReadOnlyList<T> items, Func<T, double> selector, SummaryStrategy strategy, double previous, double maxDelta)
    {
        var metrics = items.Select(selector).ToArray();
        var summary = _summarizer.Summarize(metrics, strategy);
        if (!summary.IsSuccess)
            return PipelineResult<bool>.Failure(summary.Error!);
        return IsWithinThreshold(summary.Value!, previous, maxDelta);
    }
}
