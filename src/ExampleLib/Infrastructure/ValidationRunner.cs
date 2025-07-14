using ExampleLib.Domain;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Executes manual and summarisation validations for an entity.
/// </summary>
public class ValidationRunner : IValidationRunner
{
    private readonly IValidationService _validationService;
    private readonly IManualValidatorService _manualValidator;

    public ValidationRunner(IValidationService validationService, IManualValidatorService manualValidator)
    {
        _validationService = validationService;
        _manualValidator = manualValidator;
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAsync<T>(T entity, string entityId, CancellationToken cancellationToken = default)
    {
        var summaryValid = await _validationService.ValidateAndSaveAsync(entity!, entityId, cancellationToken);
        var manualValid = _manualValidator.Validate(entity!);
        return summaryValid && manualValid;
    }
}
