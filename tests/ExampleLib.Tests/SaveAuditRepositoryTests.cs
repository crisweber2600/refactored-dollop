using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ExampleLib.Tests;

public class SaveAuditRepositoryTests
{
    [Fact]
    public void GetLastAudit_ReturnsLatestEntry()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("audit-repo-test")
            .Options;

        using var context = new YourDbContext(options);
        context.Database.EnsureCreated();
        context.SaveAudits.Add(new SaveAudit
        {
            EntityType = "User",
            EntityId = "1",
            MetricValue = 1m,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
        });
        context.SaveAudits.Add(new SaveAudit
        {
            EntityType = "User",
            EntityId = "1",
            MetricValue = 2m,
            Validated = false,
            Timestamp = DateTimeOffset.UtcNow
        });
        context.SaveChanges();
        var repo = new EfSaveAuditRepository(context);
        Assert.Equal(2, context.SaveAudits.IgnoreQueryFilters().Count());
        var last = repo.GetLastAudit("User", "1");
        Assert.NotNull(last);
        Assert.Equal(2m, last!.MetricValue);
        Assert.False(last.Validated);
    }

    [Fact]
    public void AddBatchAudit_PersistsBatchSize()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("batch-audit")
            .Options;

        using var context = new YourDbContext(options);
        context.Database.EnsureCreated();
        var repo = new EfSaveAuditRepository(context);

        var audit = new SaveAudit
        {
            EntityType = "User",
            MetricValue = 3m,
            BatchSize = 5,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow
        };

        repo.AddBatchAudit(audit);

        var last = repo.GetLastBatchAudit("User");
        Assert.NotNull(last);
        Assert.Equal(5, last!.BatchSize);
    }
}
