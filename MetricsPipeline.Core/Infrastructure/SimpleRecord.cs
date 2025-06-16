using MetricsPipeline.Core;

namespace MetricsPipeline.Infrastructure;

/// <summary>
/// Simple entity used for demonstrating repository operations.
/// </summary>
public class SimpleRecord : ISoftDelete, IBaseEntity, IRootEntity
{
    /// <summary>Entity identifier.</summary>
    public int Id { get; set; }
    /// <summary>Additional information.</summary>
    public string Info { get; set; } = string.Empty;
    /// <summary>Indicates whether the record has been soft deleted.</summary>
    public bool IsDeleted { get; set; }
}
