using MetricsPipeline.Core;

namespace MetricsPipeline.Infrastructure;

public class ExtraRecord : ISoftDelete, IBaseEntity, IRootEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
