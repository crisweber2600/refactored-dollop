using Microsoft.EntityFrameworkCore;
using Plan2RepositoryUoW.Domain.Entities;

namespace Plan2RepositoryUoW.Infrastructure.Data;

public sealed class YourDbContext : ExampleData.YourDbContext
{
    public YourDbContext(DbContextOptions<ExampleData.YourDbContext> options) : base(options) { }
    
    public DbSet<Plan2RepositoryUoW.Domain.Entities.YourEntity> Plan2Entities => Set<Plan2RepositoryUoW.Domain.Entities.YourEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder b)
        => b.UseInMemoryDatabase("plan2db");

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);
        m.Entity<Plan2RepositoryUoW.Domain.Entities.YourEntity>().HasQueryFilter(e => e.Validated);
    }
}
