using MassTransit;

namespace ExampleLib.Domain;

/// <summary>
/// MassTransit consumer that validates saves against summarisation plans.
/// </summary>
public class SaveValidationConsumer<T> : IConsumer<SaveRequested<T>>
{
    private readonly ISummarisationPlanStore _planStore;
    private readonly ISaveAuditRepository _auditRepository;
    private readonly ISummarisationValidator<T> _validator;

    public SaveValidationConsumer(ISummarisationPlanStore planStore,
        ISaveAuditRepository auditRepository,
        ISummarisationValidator<T> validator)
    {
        _planStore = planStore;
        _auditRepository = auditRepository;
        _validator = validator;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<SaveRequested<T>> context)
    {
        var message = context.Message;
        var plan = _planStore.GetPlan<T>();
        var lastAudit = _auditRepository.GetLastAudit(message.EntityType, message.EntityId);
        var isValid = _validator.Validate(message.Payload!, lastAudit!, plan);

        var newAudit = new SaveAudit
        {
            EntityType = message.EntityType,
            EntityId = message.EntityId,
            MetricValue = plan.MetricSelector(message.Payload!),
            Validated = isValid,
            Timestamp = DateTimeOffset.UtcNow
        };
        _auditRepository.AddAudit(newAudit);

        var validationEvent = new SaveValidated<T>(
            message.AppName,
            message.EntityType,
            message.EntityId,
            message.Payload,
            isValid);
        await context.Publish(validationEvent);
    }
}
