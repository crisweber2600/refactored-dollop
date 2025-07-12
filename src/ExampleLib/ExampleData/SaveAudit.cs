namespace ExampleData;

public class SaveAudit : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public decimal MetricValue { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
