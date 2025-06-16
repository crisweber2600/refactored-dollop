using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MetricsPipeline.Infrastructure;

namespace MetricsPipeline.DesignTime;

/// <summary>
/// Design-time factory for creating <see cref="SummaryDbContext"/> instances.
/// </summary>
public class SummaryDbContextFactory : IDesignTimeDbContextFactory<SummaryDbContext>
{
    /// <inheritdoc />
    public SummaryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SummaryDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=MetricsPipeline;Trusted_Connection=True;");
        return new SummaryDbContext(optionsBuilder.Options);
    }
}
