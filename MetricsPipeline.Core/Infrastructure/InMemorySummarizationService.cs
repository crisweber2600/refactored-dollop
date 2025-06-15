namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;

public class InMemorySummarizationService : ISummarizationService
{
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
