using ExampleLib.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Default implementation of <see cref="IValidationService"/>.
/// </summary>
public class ValidationService : IValidationService
{
    private readonly ISummarisationPlanStore _planStore;
    private readonly ISaveAuditRepository _auditRepository;
    private readonly IServiceProvider _provider;

    public ValidationService(ISummarisationPlanStore planStore, ISaveAuditRepository auditRepository, IServiceProvider provider)
    {
        _planStore = planStore;
        _auditRepository = auditRepository;
        _provider = provider;
    }

    /// <inheritdoc />
    public Task<bool> ValidateAndSaveAsync<T>(T entity, string entityId, CancellationToken cancellationToken = default)
    {
        var plan = _planStore.GetPlan<T>();
        var validator = _provider.GetRequiredService<ISummarisationValidator<T>>();
        var previous = _auditRepository.GetLastAudit(typeof(T).Name, entityId);
        var isValid = validator.Validate(entity!, previous!, plan);

        var audit = new SaveAudit
        {
            EntityType = typeof(T).Name,
            EntityId = entityId,
            MetricValue = plan.MetricSelector(entity!),
            Validated = isValid,
            Timestamp = DateTimeOffset.UtcNow
        };
        _auditRepository.AddAudit(audit);
        return Task.FromResult(isValid);
    }
}

