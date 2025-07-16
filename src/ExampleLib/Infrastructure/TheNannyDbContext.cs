using ExampleLib.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExampleLib.Infrastructure;

public class TheNannyDbContext : DbContext
{
    public TheNannyDbContext(DbContextOptions<TheNannyDbContext> options) : base(options) { }

    public DbSet<SaveAudit> SaveAudits => Set<SaveAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Add any SaveAudit configuration here if needed
        base.OnModelCreating(modelBuilder);
    }
}
