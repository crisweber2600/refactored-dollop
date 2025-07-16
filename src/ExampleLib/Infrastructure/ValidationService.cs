using ExampleLib.Domain;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Interface for providing custom EntityId extraction logic for SaveAudit records.
/// </summary>
public interface IEntityIdProvider
{
    /// <summary>
    /// Extracts the EntityId from an entity for SaveAudit records.
    /// </summary>
    string GetEntityId<T>(T entity) where T : IValidatable, IBaseEntity, IRootEntity;

    /// <summary>
    /// Registers a custom selector for a specific entity type.
    /// </summary>
    void RegisterSelector<T>(Func<T, string> selector) where T : IValidatable, IBaseEntity, IRootEntity;
}

/// <summary>
/// Default implementation that uses the entity's Id property.
/// </summary>
public class DefaultEntityIdProvider : IEntityIdProvider
{
    public string GetEntityId<T>(T entity) where T : IValidatable, IBaseEntity, IRootEntity
    {
        return entity.Id.ToString();
    }

    public void RegisterSelector<T>(Func<T, string> selector) where T : IValidatable, IBaseEntity, IRootEntity
    {
        // Default implementation ignores custom selectors
    }
}

/// <summary>
/// Configurable EntityId provider that allows registering custom selectors for different entity types.
/// This enables SequenceValidator to work with discriminator keys (like Name, Code) instead of just Id.
/// </summary>
public class ConfigurableEntityIdProvider : IEntityIdProvider
{
    private readonly ConcurrentDictionary<Type, object> _selectors = new();

    /// <summary>
    /// Registers a custom selector for a specific entity type.
    /// </summary>
    public void RegisterSelector<T>(Func<T, string> selector) where T : IValidatable, IBaseEntity, IRootEntity
    {
        _selectors[typeof(T)] = selector;
    }

    /// <summary>
    /// Extracts the EntityId from an entity using registered selectors or falls back to Id.ToString().
    /// </summary>
    public string GetEntityId<T>(T entity) where T : IValidatable, IBaseEntity, IRootEntity
    {
        if (_selectors.TryGetValue(typeof(T), out var selectorObj) && selectorObj is Func<T, string> selector)
        {
            return selector(entity);
        }
        
        // Default fallback to Id
        return entity.Id.ToString();
    }
}

/// <summary>
/// Default implementation of <see cref="IValidationService"/>.
/// </summary>
public class ValidationService : IValidationService
{
    private readonly ISummarisationPlanStore _planStore;
    private readonly ISaveAuditRepository _auditRepository;
    private readonly IServiceProvider _provider;
    private readonly IApplicationNameProvider _appNameProvider;
    private readonly IEntityIdProvider _entityIdProvider;

    public ValidationService(
        ISummarisationPlanStore planStore,
        ISaveAuditRepository auditRepository,
        IServiceProvider provider,
        IApplicationNameProvider appNameProvider,
        IEntityIdProvider entityIdProvider)
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
        var entityId = _entityIdProvider.GetEntityId(entity);
        var plan = _planStore.GetPlan<T>();
        var validator = _provider.GetRequiredService<ISummarisationValidator<T>>();
        var previous = _auditRepository.GetLastAudit(typeof(T).Name, entityId);
        var isValid = validator.Validate(entity!, previous!, plan);

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
        _auditRepository.AddAudit(audit);
        return Task.FromResult(isValid);
    }
}

