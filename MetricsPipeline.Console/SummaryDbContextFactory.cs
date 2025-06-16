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
        optionsBuilder.UseSqlite("Data Source=metrics.db");
        return new SummaryDbContext(optionsBuilder.Options);
    }
}
