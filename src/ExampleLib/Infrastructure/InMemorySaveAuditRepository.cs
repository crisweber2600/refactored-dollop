using System.Collections.Concurrent;
using ExampleLib.Domain;

namespace ExampleLib.Infrastructure;

/// <summary>
/// In-memory implementation of <see cref="ISaveAuditRepository"/> for testing.
/// Stores only the latest <see cref="SaveAudit"/> per entity.
/// </summary>
public class InMemorySaveAuditRepository : ISaveAuditRepository
{
    private readonly ConcurrentDictionary<(string, string), SaveAudit> _audits = new();
    private const string BatchKey = "__batch__";

    /// <summary>
    /// Retrieve the most recent audit for an entity or null if none exists.
    /// </summary>
    public SaveAudit? GetLastAudit(string entityType, string entityId)
    {
        _audits.TryGetValue((entityType, entityId), out var audit);
        return audit;
    }

    /// <summary>
    /// Persist a new audit record for an entity, replacing any existing entry.
    /// </summary>
    public void AddAudit(SaveAudit audit)
    {
        var key = (audit.EntityType, audit.EntityId);
        _audits[key] = audit;
    }

    public void AddBatchAudit(SaveAudit audit)
    {
        var batch = new SaveAudit
        {
            EntityType = audit.EntityType,
            EntityId = BatchKey,
            MetricValue = audit.MetricValue,
            BatchSize = audit.BatchSize,
            Validated = audit.Validated,
            Timestamp = audit.Timestamp
        };
        AddAudit(batch);
    }

    public SaveAudit? GetLastBatchAudit(string entityType)
        => GetLastAudit(entityType, BatchKey);
}
