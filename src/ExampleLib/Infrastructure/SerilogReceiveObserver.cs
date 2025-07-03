using System;
using System.Threading.Tasks;
using MassTransit;
using Serilog;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Logs failed messages moved to the dead letter queue.
/// </summary>
public class SerilogReceiveObserver : IReceiveObserver
{
    private readonly ILogger _logger;

    public SerilogReceiveObserver(ILogger logger)
    {
        _logger = logger;
    }

    public Task PreReceive(ReceiveContext context) => Task.CompletedTask;

    public Task PostReceive(ReceiveContext context) => Task.CompletedTask;

    public Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType) where T : class => Task.CompletedTask;

    public Task ConsumeFault<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception) where T : class
    {
        _logger.Error(exception, "Poison message for {Consumer} sent to DLQ", consumerType);
        return Task.CompletedTask;
    }

    public Task ReceiveFault(ReceiveContext context, Exception exception)
    {
        _logger.Error(exception, "Failed to receive message from {InputAddress}", context.InputAddress);
        return Task.CompletedTask;
    }
}
