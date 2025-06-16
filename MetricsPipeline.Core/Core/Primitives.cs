namespace MetricsPipeline.Core
{
    public readonly record struct Unit;

    public record PipelineResult<T>(T? Value, bool IsSuccess, string? Error = null)
    {
        public static PipelineResult<T> Success(T value) => new(value, true, null);
        public static PipelineResult<T> Failure(string error) => new(default, false, error);
    }

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
