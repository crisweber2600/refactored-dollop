namespace ExampleLib.Domain;

/// <summary>
/// Record of a save operation for auditing purposes.
/// Stores the last computed metric and validation result for an entity instance.
/// </summary>
public class SaveAudit
{
    /// <summary>Database identifier used by EF Core.</summary>
    public int Id { get; set; }

    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public decimal MetricValue { get; set; }
    /// <summary>
    /// Additional metric used by svc2 comparisons.
    /// </summary>
    public decimal Jar { get; set; }
    /// <summary>
    /// Number of entities processed in the related operation.
    /// </summary>
    public int BatchSize { get; set; }
    public bool Validated { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
