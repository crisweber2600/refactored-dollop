namespace MetricsPipeline.Core;

/// <summary>
/// Determines which gather and worker service implementations should be used.
/// </summary>
public enum PipelineMode
{
    /// <summary>Use in-memory implementations.</summary>
    InMemory,
    /// <summary>Use HTTP-based implementations.</summary>
    Http
}
