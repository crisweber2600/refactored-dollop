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
    private readonly IApplicationNameProvider _appNameProvider;
    private readonly IEntityIdProvider? _entityIdProvider;

    public ValidationService(
        ISummarisationPlanStore planStore,
        ISaveAuditRepository auditRepository,
        IServiceProvider provider,
        IApplicationNameProvider appNameProvider,
        IEntityIdProvider? entityIdProvider = null)
    {
        _planStore = planStore;
        _auditRepository = auditRepository;
        _provider = provider;
        _appNameProvider = appNameProvider;
        _entityIdProvider = entityIdProvider;
    }

    /// <inheritdoc />
    public Task<bool> ValidateAndSaveAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : IValidatable, IBaseEntity, IRootEntity
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        try
        {
            // Check if summarisation plan exists
            if (!_planStore.HasPlan<T>())
            {
                // No summarisation plan exists, validation passes by default (graceful degradation)
                return Task.FromResult(true);
            }

            var plan = _planStore.GetPlan<T>();
            if (plan == null)
            {
                return Task.FromResult(true);
            }

            // Use EntityIdProvider if available, otherwise fall back to Id.ToString()
            var entityId = _entityIdProvider?.GetEntityId(entity) ?? entity.Id.ToString();
            
            // Always try to get the previous audit record for consistency
            var previous = _auditRepository.GetLastAudit(typeof(T).Name, entityId);
            
            var validator = _provider.GetService<ISummarisationValidator<T>>();
            
            bool isValid;
            if (validator == null)
            {
                // If validator is not available, assume validation passes (graceful degradation)
                isValid = true;
            }
            else
            {
                // Use the validator to determine if the entity is valid
                isValid = validator.Validate(entity!, previous, plan);
            }

            // Create audit record - this might throw if ApplicationName or MetricSelector throw
            var audit = new SaveAudit
            {
                EntityType = typeof(T).Name,
                EntityId = entityId,
                ApplicationName = _appNameProvider.ApplicationName,
                MetricValue = plan.MetricSelector(entity!),
                BatchSize = 1,
                Validated = isValid,
                Timestamp = DateTimeOffset.UtcNow
            };
            
            // Add audit record - this might throw
            _auditRepository.AddAudit(audit);
            
            return Task.FromResult(isValid);
        }
        catch (ArgumentNullException)
        {
            throw; // Re-throw ArgumentNullException
        }
        catch
        {
            // If validation fails due to configuration issues or other exceptions,
            // we gracefully return false (validation fails) to ensure predictable behavior
            return Task.FromResult(false);
        }
    }
}

