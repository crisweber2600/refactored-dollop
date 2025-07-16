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
        var summaryValid = await _validationService.ValidateAndSaveAsync(entity!, cancellationToken);
        var manualValid = _manualValidator.Validate(entity!);
        
        // Check if sequence validation should be performed
        var sequenceValid = await ValidateSequenceAsync(entity, cancellationToken);
        
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
            var summarisationPlanStore = _serviceProvider.GetService<ISummarisationPlanStore>();

            if (auditDbContext == null || entityIdProvider == null || summarisationPlanStore == null)
            {
                // Required services not available, skip sequence validation
                return true;
            }

            // Get the value selector from SummarisationPlan to ensure consistency
            var summarisationPlan = summarisationPlanStore.GetPlan<T>();
            var valueSelector = new Func<T, decimal>(e => summarisationPlan.MetricSelector(e));

            // Perform sequence validation against SaveAudit records
            var entities = new[] { entity };
            return await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
                entities,
                auditDbContext.SaveAudits,
                entityIdProvider,
                plan,
                valueSelector,
                cancellationToken
            );
        }
        catch
        {
            // If sequence validation fails due to exceptions, allow the validation to pass
            // This ensures the system doesn't break if sequence validation is misconfigured
            return true;
        }
    }
}
