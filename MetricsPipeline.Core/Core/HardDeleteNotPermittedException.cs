namespace MetricsPipeline.Core;

/// <summary>
/// Exception thrown when a hard delete operation is attempted but not allowed.
/// </summary>
public class HardDeleteNotPermittedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HardDeleteNotPermittedException"/> class.
    /// </summary>
    public HardDeleteNotPermittedException() : base("Hard delete is not permitted") {}
}
