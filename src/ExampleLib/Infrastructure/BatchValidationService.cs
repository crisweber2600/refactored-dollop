using ExampleLib.Domain;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Default implementation of <see cref="IBatchValidationService"/>.
/// Validates that the incoming batch size is within 10% of the
/// previously recorded size for the entity type.
/// </summary>
public class BatchValidationService : IBatchValidationService
{
    private readonly ISaveAuditRepository _auditRepository;

    public BatchValidationService(ISaveAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    /// <inheritdoc />
    public bool ValidateAndAudit<T>(int batchSize)
    {
        var previous = _auditRepository.GetLastBatchAudit(typeof(T).Name);
        var isValid = previous == null || previous.BatchSize == 0 ||
                       Math.Abs(batchSize - previous.BatchSize) <= previous.BatchSize * 0.1m;
        if (isValid)
        {
            var audit = new SaveAudit
            {
                EntityType = typeof(T).Name,
                MetricValue = 0m,
                BatchSize = batchSize,
                Validated = true,
                Timestamp = DateTimeOffset.UtcNow
            };
            _auditRepository.AddBatchAudit(audit);
        }
        return isValid;
    }
}
