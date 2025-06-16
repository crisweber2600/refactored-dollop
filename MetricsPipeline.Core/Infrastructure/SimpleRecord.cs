using MetricsPipeline.Core;

namespace MetricsPipeline.Infrastructure;

public class SimpleRecord : ISoftDelete, IBaseEntity, IRootEntity
{
    public int Id { get; set; }
    public string Info { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
