using ExampleLib.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExampleLib.Infrastructure;

public class TheNannyDbContext : DbContext
{
    public TheNannyDbContext(DbContextOptions<TheNannyDbContext> options) : base(options) { }

    public DbSet<SaveAudit> SaveAudits => Set<SaveAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure SaveAudit entity
        modelBuilder.Entity<SaveAudit>(entity =>
        {
            // Configure MetricValue decimal with explicit precision and scale
            // Using precision 18 and scale 6 to accommodate a wide range of values
            entity.Property(e => e.MetricValue)
                .HasPrecision(18, 6);
            
            // Configure other properties as needed
            entity.Property(e => e.EntityType)
                .IsRequired()
                .HasMaxLength(256);
                
            entity.Property(e => e.EntityId)
                .IsRequired()
                .HasMaxLength(256);
                
            entity.Property(e => e.ApplicationName)
                .IsRequired()
                .HasMaxLength(256);
        });

        base.OnModelCreating(modelBuilder);
    }
}
