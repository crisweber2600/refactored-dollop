namespace ExampleLib.Domain;

/// <summary>
/// Repository for saving and retrieving SaveAudit records for entities.
/// </summary>
public interface ISaveAuditRepository
{
    /// <summary>Retrieve the latest SaveAudit for a given entity (by type and ID), or null if none exists.</summary>
    SaveAudit? GetLastAudit(string entityType, string entityId);

    /// <summary>Persist a new SaveAudit record.</summary>
    void AddAudit(SaveAudit audit);
}
