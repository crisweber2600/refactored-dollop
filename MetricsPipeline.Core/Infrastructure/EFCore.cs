namespace MetricsPipeline.Infrastructure;
using System.Reflection;
using System.Linq.Expressions;
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
