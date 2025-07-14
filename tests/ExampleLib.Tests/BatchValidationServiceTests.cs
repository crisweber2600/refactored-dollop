using ExampleLib.Domain;
using ExampleLib.Infrastructure;

namespace ExampleLib.Tests;

public class BatchValidationServiceTests
{
    [Fact]
    public void FirstBatch_IsAlwaysValid()
    {
        var repo = new InMemorySaveAuditRepository();
        var service = new BatchValidationService(repo);

        var result = service.ValidateAndAudit<object>(10);

        Assert.True(result);
        var audit = repo.GetLastBatchAudit(typeof(object).Name);
        Assert.NotNull(audit);
        Assert.Equal(10, audit!.BatchSize);
    }

    [Fact]
    public void WithinTenPercent_PersistsAudit()
    {
        var repo = new InMemorySaveAuditRepository();
        repo.AddBatchAudit(new SaveAudit
        {
            EntityType = typeof(object).Name,
            BatchSize = 10,
            MetricValue = 0,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow
        });
        var service = new BatchValidationService(repo);

        var result = service.ValidateAndAudit<object>(11);

        Assert.True(result);
        var audit = repo.GetLastBatchAudit(typeof(object).Name);
        Assert.Equal(11, audit!.BatchSize);
    }

    [Fact]
    public void OverTenPercent_FailsWithoutAudit()
    {
        var repo = new InMemorySaveAuditRepository();
        repo.AddBatchAudit(new SaveAudit
        {
            EntityType = typeof(object).Name,
            BatchSize = 10,
            MetricValue = 0,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow
        });
        var service = new BatchValidationService(repo);

        var result = service.ValidateAndAudit<object>(12);

        Assert.False(result);
        var audit = repo.GetLastBatchAudit(typeof(object).Name);
        Assert.Equal(10, audit!.BatchSize);
    }
}
