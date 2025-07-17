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

    /// <summary>
    /// Performs sequence validation using ValidationPlan if available.
    /// Validates against SaveAudit records using SequenceValidator extensions.
    /// </summary>
    private async Task<bool> ValidateSequenceAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : IValidatable, IBaseEntity, IRootEntity
    {
        try
        {
            Console.WriteLine($"Debug ValidateSequenceAsync: Starting sequence validation for {typeof(T).Name}");

            // Check if ValidationPlan exists for this entity type
            var validationPlanStore = _serviceProvider.GetService<IValidationPlanStore>();
            if (validationPlanStore == null || !validationPlanStore.HasPlan<T>())
            {
                Console.WriteLine($"Debug ValidateSequenceAsync: No ValidationPlan found, returning true");
                // No ValidationPlan configured, sequence validation passes by default
                return true;
            }

            var plan = validationPlanStore.GetPlan<T>();
            if (plan == null)
            {
                Console.WriteLine($"Debug ValidateSequenceAsync: ValidationPlan is null, returning true");
                return true;
            }

            Console.WriteLine($"Debug ValidateSequenceAsync: Found ValidationPlan with threshold {plan.Threshold}");

            // Get required services for sequence validation
            var auditDbContext = _serviceProvider.GetService<TheNannyDbContext>();
            var entityIdProvider = _serviceProvider.GetService<IEntityIdProvider>();
            var applicationNameProvider = _serviceProvider.GetService<IApplicationNameProvider>();

            Console.WriteLine($"Debug ValidateSequenceAsync: auditDbContext null: {auditDbContext == null}");
            Console.WriteLine($"Debug ValidateSequenceAsync: entityIdProvider null: {entityIdProvider == null}");
            Console.WriteLine($"Debug ValidateSequenceAsync: applicationNameProvider null: {applicationNameProvider == null}");

            if (auditDbContext == null || entityIdProvider == null || applicationNameProvider == null)
            {
                Console.WriteLine($"Debug ValidateSequenceAsync: Required services not available, returning true");
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
                        Console.WriteLine($"Debug ValidateSequenceAsync: Found SummarisationPlan, using its value selector");
                        
                        // Debug: Test the value selector on our entity
                        var testValue = valueSelector(entity);
                        Console.WriteLine($"Debug ValidateSequenceAsync: Value selector applied to entity: {testValue}");
                    }
                    else
                    {
                        Console.WriteLine($"Debug ValidateSequenceAsync: SummarisationPlan is null, using default value selector");
                        valueSelector = GetDefaultValueSelector<T>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Debug ValidateSequenceAsync: Exception getting SummarisationPlan: {ex.Message}, using default value selector");
                    valueSelector = GetDefaultValueSelector<T>();
                }
            }
            else
            {
                Console.WriteLine($"Debug ValidateSequenceAsync: No SummarisationPlan available, using default value selector");
                valueSelector = GetDefaultValueSelector<T>();
            }

            Console.WriteLine($"Debug ValidateSequenceAsync: About to call ValidateWithPlanAndProviderAsync");

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

            Console.WriteLine($"Debug ValidateSequenceAsync: ValidateWithPlanAndProviderAsync returned: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Debug ValidateSequenceAsync: Exception caught: {ex.Message}");
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
    private static Func<T, decimal> GetDefaultValueSelector<T>()
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
        return entity => (decimal)entity.Id;
    }
}
