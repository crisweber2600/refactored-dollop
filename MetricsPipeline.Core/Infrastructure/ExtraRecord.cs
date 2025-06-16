using MetricsPipeline.Core;

namespace MetricsPipeline.Infrastructure;

/// <summary>
/// Example entity used by integration tests.
/// </summary>
public class ExtraRecord : ISoftDelete, IBaseEntity, IRootEntity
{
    /// <summary>Entity identifier.</summary>
    public int Id { get; set; }
    /// <summary>Name of the record.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Indicates whether the record has been soft deleted.</summary>
    public bool IsDeleted { get; set; }
}
