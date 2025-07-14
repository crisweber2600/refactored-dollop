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
    private readonly IManualValidatorService _manualValidator;

    public ValidationService(ISummarisationPlanStore planStore, ISaveAuditRepository auditRepository, IServiceProvider provider)
    {
        _planStore = planStore;
        _auditRepository = auditRepository;
        _provider = provider;
        _manualValidator = provider.GetRequiredService<IManualValidatorService>();
    }

    /// <inheritdoc />
    public Task<bool> ValidateAndSaveAsync<T>(T entity, string entityId, CancellationToken cancellationToken = default)
    {
        var manualValid = _manualValidator.Validate(entity!);

        var plan = _planStore.GetPlan<T>();
        var validator = _provider.GetRequiredService<ISummarisationValidator<T>>();
        var previous = _auditRepository.GetLastAudit(typeof(T).Name, entityId);
        var summaryValid = validator.Validate(entity!, previous!, plan);
        var isValid = manualValid && summaryValid;

        var audit = new SaveAudit
        {
            EntityType = typeof(T).Name,
            EntityId = entityId,
            MetricValue = plan.MetricSelector(entity!),
            Jar = GetJarValue(entity!),
            BatchSize = 1,
            Validated = isValid,
            Timestamp = DateTimeOffset.UtcNow
        };
        _auditRepository.AddAudit(audit);
        return Task.FromResult(isValid);
    }

    private static decimal GetJarValue(object entity)
    {
        var prop = entity.GetType().GetProperty("Jar");
        var value = prop?.GetValue(entity);
        if (value != null && decimal.TryParse(value.ToString(), out var d))
            return d;
        return 0m;
    }
}

