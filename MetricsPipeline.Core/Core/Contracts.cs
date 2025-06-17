namespace MetricsPipeline.Core
{
    /// <summary>
    /// Strategy used to summarize a collection of metrics.
    /// </summary>
    public enum SummaryStrategy { Average, Sum, Count }

    /// <summary>
    /// Mode controlling how metrics are gathered and processed.
    /// </summary>
    public enum WorkerMode { InMemory, Http }

    /// <summary>
    /// Service responsible for retrieving raw metric values.
    /// </summary>
    public interface IGatherService
    {
        /// <summary>
        /// Retrieves metrics from the specified source.
        /// </summary>
        /// <param name="source">Endpoint containing the metrics.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>The fetched metrics wrapped in a pipeline result.</returns>
        Task<PipelineResult<IReadOnlyList<double>>> FetchMetricsAsync(Uri source, CancellationToken ct = default);
    }

    /// <summary>
    /// Generic worker capable of retrieving typed items from a source.
    /// </summary>
    public interface IWorkerService
    {
        /// <summary>
        /// Fetches a collection of items from the specified source.
        /// </summary>
        /// <typeparam name="T">Item type to deserialize.</typeparam>
        /// <param name="source">Endpoint containing the data.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>The fetched items wrapped in a pipeline result.</returns>
        Task<PipelineResult<IReadOnlyList<T>>> FetchAsync<T>(Uri source, CancellationToken ct = default);
    }

    /// <summary>
    /// Service that summarizes a set of metrics.
    /// </summary>
    public interface ISummarizationService
    {
        /// <summary>
        /// Summarizes the provided metrics using the given strategy.
        /// </summary>
        /// <param name="metrics">Metric values to summarize.</param>
        /// <param name="strategy">Strategy used to summarize the values.</param>
        /// <returns>The summary wrapped in a pipeline result.</returns>
        PipelineResult<double> Summarize(IReadOnlyList<double> metrics, SummaryStrategy strategy);
    }

    /// <summary>
    /// Validates whether a summary value is acceptable.
    /// </summary>
    public interface IValidationService
    {
        /// <summary>
        /// Checks the delta between current and previous summaries.
        /// </summary>
        /// <param name="current">Current summary value.</param>
        /// <param name="previous">Last committed summary value.</param>
        /// <param name="maxDelta">Allowed difference between values.</param>
        /// <returns>True when the delta is within range.</returns>
        PipelineResult<bool> IsWithinThreshold(double current, double previous, double maxDelta);

        /// <summary>
        /// Calculates a summary from a list of items and validates the delta.
        /// </summary>
        /// <typeparam name="T">Item type.</typeparam>
        /// <param name="items">Collection of items.</param>
        /// <param name="selector">Expression selecting the value to summarise.</param>
        /// <param name="strategy">Summarisation strategy.</param>
        /// <param name="previous">Last committed summary value.</param>
        /// <param name="maxDelta">Allowed difference between values.</param>
        /// <returns>True when the calculated summary is within range.</returns>
        PipelineResult<bool> IsWithinThreshold<T>(IReadOnlyList<T> items, Func<T, double> selector, SummaryStrategy strategy, double previous, double maxDelta);
    }

    /// <summary>
    /// Persists summary information for a pipeline.
    /// </summary>
    public interface ICommitService
    {
        /// <summary>
        /// Commits a summary value.
        /// </summary>
        /// <param name="pipelineName">Name of the pipeline.</param>
        /// <param name="source">Source that produced the metrics.</param>
        /// <param name="summary">Calculated summary.</param>
        /// <param name="ts">Timestamp of the commit.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>Operation success indicator.</returns>
        Task<PipelineResult<Unit>> CommitAsync(string pipelineName, Uri source, double summary, DateTime ts, CancellationToken ct = default);
    }

    /// <summary>
    /// Handles discarded summaries.
    /// </summary>
    public interface IDiscardHandler
    {
        /// <summary>
        /// Performs actions when a summary is discarded.
        /// </summary>
        /// <param name="summary">Summary value.</param>
        /// <param name="reason">Reason for discard.</param>
        /// <param name="ct">Optional cancellation token.</param>
        Task HandleDiscardAsync(double summary, string reason, CancellationToken ct = default);
    }

    /// <summary>
    /// Repository used to store summary records.
    /// </summary>
    public interface ISummaryRepository
    {
        /// <summary>
        /// Retrieves the most recently committed summary for a pipeline.
        /// </summary>
        /// <param name="pipelineName">Pipeline name.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>The last committed value wrapped in a pipeline result.</returns>
        Task<PipelineResult<double>> GetLastCommittedAsync(string pipelineName, CancellationToken ct = default);

        /// <summary>
        /// Persists a summary record.
        /// </summary>
        /// <param name="pipelineName">Pipeline name.</param>
        /// <param name="source">Source that produced the metrics.</param>
        /// <param name="summary">Summary value.</param>
        /// <param name="ts">Timestamp of the record.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>Operation success indicator.</returns>
        Task<PipelineResult<Unit>> SaveAsync(string pipelineName, Uri source, double summary, DateTime ts, CancellationToken ct = default);
    }

    /// <summary>
    /// Executes all steps of the metrics pipeline.
    /// </summary>
    public interface IPipelineOrchestrator
    {
        /// <summary>
        /// Runs the pipeline for the given source using the specified strategy.
        /// </summary>
        /// <param name="pipelineName">Pipeline name.</param>
        /// <param name="source">Source endpoint.</param>
        /// <param name="strategy">Summarization strategy.</param>
        /// <param name="threshold">Maximum allowed delta.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>The resulting pipeline state.</returns>
        Task<PipelineResult<PipelineState<T>>> ExecuteAsync<T>(
            string pipelineName,
            Uri source,
            Func<T, double> selector,
            SummaryStrategy strategy,
            double threshold,
            CancellationToken ct = default,
            string workerMethod = nameof(IWorkerService.FetchAsync));
    }
}
