using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExampleLib.Domain;

/// <summary>
/// Record of a save operation for auditing purposes.
/// Stores the last computed metric and validation result for an entity instance.
/// </summary>
public class SaveAudit
{
    /// <summary>Database identifier used by EF Core.</summary>
    [BsonIgnore] // Ignore for MongoDB, used only by EF
    public int Id { get; set; }

    [BsonId] // Use this as the MongoDB _id
    [BsonRepresentation(BsonType.ObjectId)]
    public string? MongoId { get; set; }

    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public decimal MetricValue { get; set; }
    /// <summary>
    /// Number of entities processed in the related operation.
    /// </summary>
    public int BatchSize { get; set; }
    public bool Validated { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
