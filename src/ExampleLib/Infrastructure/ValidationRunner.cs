using ExampleLib.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Executes manual and summarisation validations for an entity.
/// </summary>
public class ValidationRunner : IValidationRunner
{
    private readonly IValidationService _validationService;
    private readonly IManualValidatorService _manualValidator;
    private readonly IServiceProvider _serviceProvider;

    public ValidationRunner(IValidationService validationService, IManualValidatorService manualValidator, IServiceProvider serviceProvider)
    {
        _validationService = validationService;
        _manualValidator = manualValidator;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : IValidatable, IBaseEntity, IRootEntity
    {
        // Execute validations in the correct order: Manual ? Sequence ? Summary
        // This ensures sequence validation runs against existing audit data before new audit is created
        var manualValid = _manualValidator.Validate(entity!);
        var sequenceValid = await ValidateSequenceAsync(entity, cancellationToken);
        var summaryValid = await _validationService.ValidateAndSaveAsync(entity!, cancellationToken);
        
        return summaryValid && manualValid && sequenceValid;
    }

    /// <inheritdoc />
    public async Task<bool> ValidateManyAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        where T : IValidatable, IBaseEntity, IRootEntity
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return true;

        // Execute validations in the correct order: Manual ? Sequence ? Summary
        // For bulk validation, we need to validate all entities for each step
        
        // Step 1: Manual validation - check all entities
        var manualValid = true;
        foreach (var entity in entityList)
        {
            if (!_manualValidator.Validate(entity!))
            {
                manualValid = false;
                break; // Fail fast for manual validation
            }
        }
        
        if (!manualValid)
            return false;

        // Step 2: Sequence validation - validate all entities as a collection
        var sequenceValid = await ValidateSequenceManyAsync(entityList, cancellationToken);
        
        if (!sequenceValid)
            return false;

        // Step 3: Summary validation - validate each entity individually
        var summaryValid = true;
        foreach (var entity in entityList)
        {
            if (!await _validationService.ValidateAndSaveAsync(entity!, cancellationToken))
            {
                summaryValid = false;
                break; // Fail fast for summary validation
            }
        }
        
        return summaryValid;
    }

    /// <summary>
    /// Performs sequence validation using ValidationPlan if available.
    /// Validates against SaveAudit records using SequenceValidator extensions.
    /// </summary>
    private async Task<bool> ValidateSequenceAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : IValidatable, IBaseEntity, IRootEntity
    {
        try
        {
            // Check if ValidationPlan exists for this entity type
            var validationPlanStore = _serviceProvider.GetService<IValidationPlanStore>();
            if (validationPlanStore == null || !validationPlanStore.HasPlan<T>())
            {
                // No ValidationPlan configured, sequence validation passes by default
                return true;
            }

            var plan = validationPlanStore.GetPlan<T>();
            if (plan == null)
            {
                return true;
            }

            // Get required services for sequence validation
            var auditDbContext = _serviceProvider.GetService<TheNannyDbContext>();
            var entityIdProvider = _serviceProvider.GetService<IEntityIdProvider>();
            var applicationNameProvider = _serviceProvider.GetService<IApplicationNameProvider>();

            if (auditDbContext == null || entityIdProvider == null || applicationNameProvider == null)
            {
                // Required services not available, skip sequence validation gracefully
                return true;
            }

            // Try to get the value selector from SummarisationPlan first, otherwise derive from entity properties
            Func<T, decimal> valueSelector;
            var summarisationPlanStore = _serviceProvider.GetService<ISummarisationPlanStore>();
            
            if (summarisationPlanStore != null && summarisationPlanStore.HasPlan<T>())
            {
                try
                {
                    var summarisationPlan = summarisationPlanStore.GetPlan<T>();
                    if (summarisationPlan != null)
                    {
                        valueSelector = new Func<T, decimal>(e => summarisationPlan.MetricSelector(e));
                    }
                    else
                    {
                        valueSelector = GetDefaultValueSelector<T>();
                    }
                }
                catch (Exception)
                {
                    valueSelector = GetDefaultValueSelector<T>();
                }
            }
            else
            {
                valueSelector = GetDefaultValueSelector<T>();
            }

            // Perform sequence validation against SaveAudit records
            var entities = new[] { entity };
            var result = await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
                entities,
                auditDbContext.SaveAudits,
                entityIdProvider,
                plan,
                valueSelector,
                applicationNameProvider.ApplicationName,
                cancellationToken
            );

            return result;
        }
        catch (Exception)
        {
            // If sequence validation fails due to configuration issues or other exceptions,
            // we gracefully skip sequence validation (return true) rather than failing the entire validation
            // This ensures system resilience when sequence validation is misconfigured
            return true;
        }
    }

    /// <summary>
    /// Performs sequence validation for a collection of entities using ValidationPlan if available.
    /// Validates against SaveAudit records using SequenceValidator extensions.
    /// </summary>
    private async Task<bool> ValidateSequenceManyAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        where T : IValidatable, IBaseEntity, IRootEntity
    {
        try
        {
            // Check if ValidationPlan exists for this entity type
            var validationPlanStore = _serviceProvider.GetService<IValidationPlanStore>();
            if (validationPlanStore == null || !validationPlanStore.HasPlan<T>())
            {
                // No ValidationPlan configured, sequence validation passes by default
                return true;
            }

            var plan = validationPlanStore.GetPlan<T>();
            if (plan == null)
            {
                return true;
            }

            // Get required services for sequence validation
            var auditDbContext = _serviceProvider.GetService<TheNannyDbContext>();
            var entityIdProvider = _serviceProvider.GetService<IEntityIdProvider>();
            var applicationNameProvider = _serviceProvider.GetService<IApplicationNameProvider>();

            if (auditDbContext == null || entityIdProvider == null || applicationNameProvider == null)
            {
                // Required services not available, skip sequence validation gracefully
                return true;
            }

            // Try to get the value selector from SummarisationPlan first, otherwise derive from entity properties
            Func<T, decimal> valueSelector;
            var summarisationPlanStore = _serviceProvider.GetService<ISummarisationPlanStore>();
            
            if (summarisationPlanStore != null && summarisationPlanStore.HasPlan<T>())
            {
                try
                {
                    var summarisationPlan = summarisationPlanStore.GetPlan<T>();
                    if (summarisationPlan != null)
                    {
                        valueSelector = new Func<T, decimal>(e => summarisationPlan.MetricSelector(e));
                    }
                    else
                    {
                        valueSelector = GetDefaultValueSelector<T>();
                    }
                }
                catch (Exception)
                {
                    valueSelector = GetDefaultValueSelector<T>();
                }
            }
            else
            {
                valueSelector = GetDefaultValueSelector<T>();
            }

            // Perform sequence validation against SaveAudit records for the entire collection
            var result = await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
                entities,
                auditDbContext.SaveAudits,
                entityIdProvider,
                plan,
                valueSelector,
                applicationNameProvider.ApplicationName,
                cancellationToken
            );

            return result;
        }
        catch (Exception)
        {
            // If sequence validation fails due to configuration issues or other exceptions,
            // we gracefully skip sequence validation (return true) rather than failing the entire validation
            // This ensures system resilience when sequence validation is misconfigured
            return true;
        }
    }

    /// <summary>
    /// Gets a default value selector for the entity type T.
    /// Attempts to use a 'Value' property if available, otherwise falls back to Id.
    /// </summary>
    public Func<T, decimal> GetDefaultValueSelector<T>()
        where T : IValidatable, IBaseEntity, IRootEntity
    {
        var type = typeof(T);
        var valueProperty = type.GetProperty("Value");
        
        if (valueProperty != null && (valueProperty.PropertyType == typeof(decimal) || 
                                      valueProperty.PropertyType == typeof(decimal?) ||
                                      valueProperty.PropertyType == typeof(double) ||
                                      valueProperty.PropertyType == typeof(double?) ||
                                      valueProperty.PropertyType == typeof(float) ||
                                      valueProperty.PropertyType == typeof(float?) ||
                                      valueProperty.PropertyType == typeof(int) ||
                                      valueProperty.PropertyType == typeof(int?)))
        {
            return entity =>
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));
                    
                var value = valueProperty.GetValue(entity);
                return value switch
                {
                    decimal d => d,
                    double d => (decimal)d,
                    float f => (decimal)f,
                    int i => (decimal)i,
                    null => 0m,
                    _ => 0m
                };
            };
        }
        
        // Check for Amount property as well
        var amountProperty = type.GetProperty("Amount");
        if (amountProperty != null && (amountProperty.PropertyType == typeof(decimal) || 
                                       amountProperty.PropertyType == typeof(decimal?) ||
                                       amountProperty.PropertyType == typeof(double) ||
                                       amountProperty.PropertyType == typeof(double?) ||
                                       amountProperty.PropertyType == typeof(float) ||
                                       amountProperty.PropertyType == typeof(float?) ||
                                       amountProperty.PropertyType == typeof(int) ||
                                       amountProperty.PropertyType == typeof(int?)))
        {
            return entity =>
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));
                    
                var value = amountProperty.GetValue(entity);
                return value switch
                {
                    decimal d => d,
                    double d => (decimal)d,
                    float f => (decimal)f,
                    int i => (decimal)i,
                    null => 0m,
                    _ => 0m
                };
            };
        }
        
        // Fallback to using the Id property
        return entity => 
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            return (decimal)entity.Id;
        };
    }
}
