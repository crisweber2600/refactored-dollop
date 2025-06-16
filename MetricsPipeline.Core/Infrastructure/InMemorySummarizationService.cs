namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;

/// <summary>
/// Summarization service that calculates results in memory.
/// </summary>
public class InMemorySummarizationService : ISummarizationService
{
    /// <inheritdoc />
    public PipelineResult<double> Summarize(IReadOnlyList<double> metrics, SummaryStrategy strategy)
    {
        if (metrics == null || metrics.Count == 0)
            return PipelineResult<double>.Failure("NoData");

        return strategy switch
        {
            SummaryStrategy.Average => PipelineResult<double>.Success(metrics.Average()),
            SummaryStrategy.Sum     => PipelineResult<double>.Success(metrics.Sum()),
            SummaryStrategy.Count   => PipelineResult<double>.Success(metrics.Count),
            _ => PipelineResult<double>.Failure("UnknownStrategy")
        };
    }
}
