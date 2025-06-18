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
}
