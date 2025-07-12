using ExampleLib.Messages;
using MassTransit;
using ExampleLib;

namespace ExampleLib.Domain;

/// <summary>
/// Consumes <see cref="SaveValidated{T}"/> messages and records a commit audit.
/// Publishes a <see cref="SaveCommitFault{T}"/> when processing fails.
/// </summary>
public class SaveCommitConsumer<T> : IConsumer<SaveValidated<T>>
{
    private readonly IValidationPlanProvider _planStore;
    private readonly ISaveAuditRepository _auditRepository;

    public SaveCommitConsumer(IValidationPlanProvider planStore, ISaveAuditRepository auditRepository)
    {
        _planStore = planStore;
        _auditRepository = auditRepository;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<SaveValidated<T>> context)
    {
        var message = context.Message;
        try
        {
            var plan = _planStore.GetPlan<T>();
            var audit = new SaveAudit
            {
                EntityType = message.EntityType,
                EntityId = message.EntityId,
                MetricValue = plan.MetricSelector(message.Payload!),
                Validated = message.Validated,
                Timestamp = DateTimeOffset.UtcNow
            };
            _auditRepository.AddAudit(audit);
        }
        catch (Exception ex)
        {
            await context.Publish(new SaveCommitFault<T>(
                message.AppName,
                message.EntityType,
                message.EntityId,
                message.Payload,
                ex.Message));
        }
    }
}
