using ExampleLib.Domain;
using ExampleLib.Infrastructure;

namespace ExampleLib.Tests;

public class InMemoryStoreTests
{
    [Fact]
    public void PlanStore_AddAndRetrieve_Works()
    {
        var store = new InMemorySummarisationPlanStore();
        var plan = new SummarisationPlan<string>(_ => 1m, ThresholdType.RawDifference, 0);
        store.AddPlan(plan);

        var result = store.GetPlan<string>();
        Assert.Equal(plan, result);
    }

    [Fact]
    public void SaveAuditRepository_AddAndRetrieve_Works()
    {
        var repo = new InMemorySaveAuditRepository();
        var audit = new SaveAudit { EntityType = "User", EntityId = "1", MetricValue = 5m };
        repo.AddAudit(audit);

        var result = repo.GetLastAudit("User", "1");
        Assert.Equal(audit, result);
    }
}
