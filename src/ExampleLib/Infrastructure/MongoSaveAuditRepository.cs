using ExampleLib.Domain;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ExampleLib.Infrastructure;

/// <summary>
/// MongoDB implementation of <see cref="ISaveAuditRepository"/>.
/// Stores audits in the "SaveAudits" collection.
/// </summary>
public class MongoSaveAuditRepository : ISaveAuditRepository
{
    private readonly IMongoCollection<SaveAudit> _collection;
    private const string BatchKey = "__batch__";

    public MongoSaveAuditRepository(IMongoClient mongoClient)
    {
        _ = mongoClient ?? throw new ArgumentNullException(nameof(mongoClient));

        var database = mongoClient.GetDatabase("ExampleLib"); // Use consistent database name
        _collection = database.GetCollection<SaveAudit>("SaveAudits");
    }

    /// <inheritdoc />
    public SaveAudit? GetLastAudit(string entityType, string entityId)
    {
        try
        {
            var filter = Builders<SaveAudit>.Filter.Eq(a => a.EntityType, entityType) &
                         Builders<SaveAudit>.Filter.Eq(a => a.EntityId, entityId);
            
            var sortDefinition = Builders<SaveAudit>.Sort.Descending(a => a.Timestamp);
            
            return _collection.Find(filter)
                .Sort(sortDefinition)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public void AddAudit(SaveAudit audit)
    {
        if (audit == null)
            throw new ArgumentNullException(nameof(audit));

        var filter = Builders<SaveAudit>.Filter.Eq(a => a.EntityType, audit.EntityType) &
                     Builders<SaveAudit>.Filter.Eq(a => a.EntityId, audit.EntityId) &
                     Builders<SaveAudit>.Filter.Eq(a => a.ApplicationName, audit.ApplicationName);

        // Try to find an existing document
        var existing = _collection.Find(filter).FirstOrDefault();
        if (existing != null)
        {
            // Keep the existing MongoId to avoid immutable _id error
            audit.MongoId = existing.MongoId;
        }
        else
        {
            // New document, generate a new MongoId if not set
            if (string.IsNullOrEmpty(audit.MongoId))
            {
                audit.MongoId = ObjectId.GenerateNewId().ToString();
            }
        }
        _collection.ReplaceOne(filter, audit, new ReplaceOptions { IsUpsert = true });
    }

    public void AddBatchAudit(SaveAudit audit)
    {
        if (audit == null)
            throw new ArgumentNullException(nameof(audit));

        var batch = new SaveAudit
        {
            EntityType = audit.EntityType,
            EntityId = BatchKey,
            ApplicationName = audit.ApplicationName,
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
