using System.Linq.Expressions;
using ExampleLib.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExampleData;

public class YourDbContext : DbContext
{
    public YourDbContext(DbContextOptions<YourDbContext> options) : base(options) { }

    public DbSet<YourEntity> YourEntities => Set<YourEntity>();
    public DbSet<Nanny> Nannies => Set<Nanny>();
    public DbSet<SaveAudit> SaveAudits => Set<SaveAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(YourDbContext).Assembly);

        foreach (var type in modelBuilder.Model.GetEntityTypes()
                     .Select(e => e.ClrType)
                     .Where(t => typeof(IValidatable).IsAssignableFrom(t) && !t.IsAbstract))
        {
            var param = Expression.Parameter(type, "e");
            var body = Expression.Equal(
                Expression.Property(param, nameof(IValidatable.Validated)),
                Expression.Constant(true));
            var lambda = Expression.Lambda(body, param);
            modelBuilder.Entity(type).HasQueryFilter(lambda);
        }
    }
}
