using ExampleLib.Messages;
using MassTransit;

namespace ExampleLib.Domain;

/// <summary>
/// Validates delete requests using <see cref="IManualValidatorService"/>.
/// </summary>
public class DeleteValidationConsumer<T> : IConsumer<DeleteRequested<T>>
{
    private readonly IManualValidatorService _validator;

    public DeleteValidationConsumer(IManualValidatorService validator)
    {
        _validator = validator;
    }

    public async Task Consume(ConsumeContext<DeleteRequested<T>> context)
    {
        var msg = context.Message;
        var valid = msg.Payload != null && _validator.Validate(msg.Payload);
        await context.Publish(new DeleteValidated<T>(msg.AppName, msg.EntityType, msg.EntityId, msg.Payload, valid));
    }
}
