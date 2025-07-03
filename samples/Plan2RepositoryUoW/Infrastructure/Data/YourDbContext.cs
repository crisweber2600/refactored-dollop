using Microsoft.EntityFrameworkCore;
using Plan2RepositoryUoW.Domain.Entities;

namespace Plan2RepositoryUoW.Infrastructure.Data;

public sealed class YourDbContext : DbContext
{
    public DbSet<YourEntity> YourEntities => Set<YourEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder b)
        => b.UseInMemoryDatabase("plan2db");

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<YourEntity>().HasQueryFilter(e => e.Validated);
    }
}
