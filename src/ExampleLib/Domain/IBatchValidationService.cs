namespace ExampleLib.Domain;

/// <summary>
/// Validates batch operations against the previous recorded batch size.
/// </summary>
public interface IBatchValidationService
{
    /// <summary>
    /// Validate the batch size for <typeparamref name="T"/> and persist a
    /// new audit record when the check succeeds.
    /// </summary>
    /// <param name="batchSize">Number of entities being saved.</param>
    /// <returns><c>true</c> when the operation is within the allowed range.</returns>
    bool ValidateAndAudit<T>(int batchSize);
}
