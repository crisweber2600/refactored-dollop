using ExampleLib.Messages;
using MassTransit;

namespace ExampleLib.Domain;

/// <summary>
/// Publishes a <see cref="DeleteCommitted{T}"/> event when validation succeeds.
/// </summary>
public class DeleteCommitConsumer<T> : IConsumer<DeleteValidated<T>>
{
    public async Task Consume(ConsumeContext<DeleteValidated<T>> context)
    {
        var msg = context.Message;
        if (msg.Validated)
        {
            await context.Publish(new DeleteCommitted<T>(msg.AppName, msg.EntityType, msg.EntityId, msg.Payload));
        }
    }
}
