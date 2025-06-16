namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;

/// <summary>
/// Discard handler that simply logs the discarded summary to the console.
/// </summary>
public class LoggingDiscardHandler : IDiscardHandler
{
    /// <inheritdoc />
    public Task HandleDiscardAsync(double summary, string reason, CancellationToken ct = default)
    {
        Console.WriteLine($"[DISCARD] {summary} – {reason}");
        return Task.CompletedTask;
    }
}
