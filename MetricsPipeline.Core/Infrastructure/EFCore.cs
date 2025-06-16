namespace MetricsPipeline.Infrastructure;
using System.Reflection;
using System.Linq.Expressions;
using MetricsPipeline.Core;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Entity used to persist calculated summaries.
/// </summary>
public class SummaryRecord : ISoftDelete, IBaseEntity, IRootEntity
{
    /// <summary>Record identifier.</summary>
    public int Id { get; set; }
    /// <summary>Name of the pipeline.</summary>
    public string PipelineName { get; set; } = string.Empty;
    /// <summary>Source that produced the metrics.</summary>
    public Uri Source { get; set; } = default!;
    /// <summary>Summary value.</summary>
    public double Value { get; set; }
    /// <summary>Time the record was created.</summary>
    public DateTime Timestamp { get; set; }
    /// <summary>Indicates if the record has been soft deleted.</summary>
    public bool IsDeleted { get; set; }
}

/// <summary>
/// Entity Framework database context for summaries.
/// </summary>
public class SummaryDbContext : DbContext
{
    /// <summary>Table of summary records.</summary>
    public DbSet<SummaryRecord> Summaries => Set<SummaryRecord>();

    /// <summary>
    /// Initializes a new instance of the context.
    /// </summary>
    public SummaryDbContext(DbContextOptions<SummaryDbContext> options) : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var entityTypes = assembly.GetTypes()
            .Where(t => typeof(IEntity).IsAssignableFrom(t)
                        && typeof(ISoftDelete).IsAssignableFrom(t)
                        && t.IsClass && !t.IsAbstract);

        foreach (var type in entityTypes)
        {
            var entity = modelBuilder.Entity(type);
            var param = Expression.Parameter(type, "e");
            var prop = Expression.Property(param, nameof(ISoftDelete.IsDeleted));
            var condition = Expression.Equal(prop, Expression.Constant(false));
            var lambda = Expression.Lambda(condition, param);
            entity.HasQueryFilter(lambda);
        }

        modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        base.OnModelCreating(modelBuilder);
    }
}
