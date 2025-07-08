using ExampleLib.Domain;
using MongoDB.Driver;

namespace ExampleLib.Infrastructure;

/// <summary>
/// MongoDB implementation of <see cref="ISaveAuditRepository"/>.
/// Stores audits in the "SaveAudits" collection.
/// </summary>
public class MongoSaveAuditRepository : ISaveAuditRepository
{
    private readonly IMongoCollection<SaveAudit> _collection;

    public MongoSaveAuditRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<SaveAudit>("SaveAudits");
    }

    /// <inheritdoc />
    public SaveAudit? GetLastAudit(string entityType, string entityId)
    {
        var filter = Builders<SaveAudit>.Filter.Eq(a => a.EntityType, entityType) &
                     Builders<SaveAudit>.Filter.Eq(a => a.EntityId, entityId);
        return _collection.Find(filter)
            .SortByDescending(a => a.Timestamp)
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public void AddAudit(SaveAudit audit)
    {
        var filter = Builders<SaveAudit>.Filter.Eq(a => a.EntityType, audit.EntityType) &
                     Builders<SaveAudit>.Filter.Eq(a => a.EntityId, audit.EntityId);
        _collection.ReplaceOne(filter, audit, new ReplaceOptions { IsUpsert = true });
    }
}
