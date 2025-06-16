namespace MetricsPipeline.Core
{
    /// <summary>
    /// Represents a void result for pipeline operations.
    /// </summary>
    public readonly record struct Unit;

    /// <summary>
    /// Generic result type used throughout the pipeline.
    /// </summary>
    /// <typeparam name="T">Wrapped value type.</typeparam>
    /// <param name="Value">Returned value when successful.</param>
    /// <param name="IsSuccess">Indicates whether the operation succeeded.</param>
    /// <param name="Error">Error message when the operation fails.</param>
    public record PipelineResult<T>(T? Value, bool IsSuccess, string? Error = null)
    {
        /// <summary>
        /// Creates a successful result.
        /// </summary>
        public static PipelineResult<T> Success(T value) => new(value, true, null);

        /// <summary>
        /// Creates a failed result with the provided error message.
        /// </summary>
        public static PipelineResult<T> Failure(string error) => new(default, false, error);
    }

    /// <summary>
    /// Captures the state of the pipeline after execution.
    /// </summary>
    /// <param name="PipelineName">Name of the pipeline.</param>
    /// <param name="SourceEndpoint">Metrics source endpoint.</param>
    /// <param name="RawMetrics">Raw metric values.</param>
    /// <param name="Summary">Calculated summary.</param>
    /// <param name="LastCommittedSummary">Last committed summary value.</param>
    /// <param name="AcceptableDelta">Maximum delta allowed.</param>
    /// <param name="Timestamp">Time of the pipeline execution.</param>
    public record PipelineState(
        string PipelineName,
        Uri SourceEndpoint,
        IReadOnlyList<double> RawMetrics,
        double? Summary,
        double? LastCommittedSummary,
        double AcceptableDelta,
        DateTime Timestamp
    );
}
