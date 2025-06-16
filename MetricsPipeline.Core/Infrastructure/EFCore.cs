namespace MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;
using Microsoft.EntityFrameworkCore;

public class SummaryRecord : ISoftDelete, IBaseEntity, IRootEntity
{
    public int Id { get; set; }
    public string PipelineName { get; set; } = string.Empty;
    public Uri Source { get; set; } = default!;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsDeleted { get; set; }
}

public class SummaryDbContext : DbContext
{
    public DbSet<SummaryRecord> Summaries => Set<SummaryRecord>();

    public SummaryDbContext(DbContextOptions<SummaryDbContext> options) : base(options) { }
}
