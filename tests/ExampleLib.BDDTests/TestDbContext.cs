using ExampleData;
using Microsoft.EntityFrameworkCore;

namespace ExampleLib.BDDTests;

public class TestDbContext : YourDbContext
{
    public TestDbContext(DbContextOptions<YourDbContext> options) : base(options) { }

    public DbSet<Foo> Foos => Set<Foo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Foo>(builder =>
        {
            builder.HasKey(f => f.Id);
            builder.Property(f => f.Description).HasMaxLength(200).IsRequired();
        });
    }
}
