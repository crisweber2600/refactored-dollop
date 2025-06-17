namespace MetricsPipeline.Infrastructure;

/// <summary>
/// Represents a hosted worker that returns a collection of items after execution.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
public interface IHostedWorker<T>
{
    /// <summary>
    /// Executes the worker job and returns the produced items.
    /// </summary>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>The items produced by the worker.</returns>
    Task<IReadOnlyList<T>> RunAsync(CancellationToken ct = default);
}
