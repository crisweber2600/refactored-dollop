namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;

public class LoggingDiscardHandler : IDiscardHandler
{
    public Task HandleDiscardAsync(double summary, string reason, CancellationToken ct = default)
    {
        Console.WriteLine($"[DISCARD] {summary} – {reason}");
        return Task.CompletedTask;
    }
}
