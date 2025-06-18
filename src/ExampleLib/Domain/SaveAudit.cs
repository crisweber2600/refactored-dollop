namespace ExampleLib.Domain;

/// <summary>
/// Record of a save operation for auditing purposes.
/// Stores the last computed metric and validation result for an entity instance.
/// </summary>
public class SaveAudit
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public decimal MetricValue { get; set; }
    public bool Validated { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
