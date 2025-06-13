using Microsoft.EntityFrameworkCore;
using refactored_dollop.Workflow;
using refactored_dollop;

namespace refactored_dollop.Data;

public class WorkflowContext : DbContext
{
    public WorkflowContext(DbContextOptions<WorkflowContext> options) : base(options) { }

    public DbSet<Foo> Foos => Set<Foo>();
    public DbSet<WorkflowRun> WorkflowRuns => Set<WorkflowRun>();
    public DbSet<WorkflowRunLog> WorkflowRunLogs => Set<WorkflowRunLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowRun>(b =>
        {
            b.ToTable("WorkflowRun");
            b.HasKey(x => x.Id);
            b.Property(x => x.Status).HasConversion<string>();
            b.HasMany(x => x.Logs)
             .WithOne(l => l.WorkflowRun)
             .HasForeignKey(l => l.WorkflowRunId);
        });

        modelBuilder.Entity<WorkflowRunLog>(b =>
        {
            b.ToTable("WorkflowRunLog");
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<Foo>(b =>
        {
            b.ToTable("Foo");
            b.HasKey(f => f.Id);
        });
    }
}
