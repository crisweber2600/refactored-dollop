namespace MetricsPipeline.Core
{
    public enum SummaryStrategy { Average, Sum, Count }

    public interface IGatherService
    {
        Task<PipelineResult<IReadOnlyList<double>>> FetchMetricsAsync(Uri source, CancellationToken ct = default);
    }

    public interface ISummarizationService
    {
        PipelineResult<double> Summarize(IReadOnlyList<double> metrics, SummaryStrategy strategy);
    }

    public interface IValidationService
    {
        PipelineResult<bool> IsWithinThreshold(double current, double previous, double maxDelta);
    }

    public interface ICommitService
    {
        Task<PipelineResult<Unit>> CommitAsync(double summary, DateTime ts, CancellationToken ct = default);
    }

    public interface IDiscardHandler
    {
        Task HandleDiscardAsync(double summary, string reason, CancellationToken ct = default);
    }

    public interface ISummaryRepository
    {
        Task<PipelineResult<double>> GetLastCommittedAsync(Uri source, CancellationToken ct = default);
        Task<PipelineResult<Unit>> SaveAsync(double summary, DateTime ts, CancellationToken ct = default);
    }

    public interface IPipelineOrchestrator
    {
        Task<PipelineResult<PipelineState>> ExecuteAsync(
            Uri source,
            SummaryStrategy strategy,
            double threshold,
            CancellationToken ct = default);
    }
}
