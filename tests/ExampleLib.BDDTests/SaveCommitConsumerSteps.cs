using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using ExampleLib.Messages;
using MassTransit.Testing;
using Reqnroll;
using Xunit;

namespace ExampleLib.BDDTests;

[Binding]
public class SaveCommitConsumerSteps
{
    private InMemoryTestHarness? _harness;
    private InMemorySaveAuditRepository? _repo;

    [Given("a SaveCommit consumer")]
    public void GivenConsumer()
    {
        var store = new InMemorySummarisationPlanStore();
        store.AddPlan(new SummarisationPlan<YourEntity>(e => e.Id, ThresholdType.RawDifference, 1));
        _repo = new InMemorySaveAuditRepository();
        _harness = new InMemoryTestHarness();
        _harness.Consumer(() => new SaveCommitConsumer<YourEntity>(store, _repo));
        _harness.Start().Wait();
    }

    [When("a valid SaveValidated message is processed")]
    public async Task WhenMessageProcessed()
    {
        if (_harness == null) throw new();
        var msg = new SaveValidated<YourEntity>("App", nameof(YourEntity), "1", new YourEntity { Id = 1 }, true);
        await _harness.InputQueueSendEndpoint.Send(msg);
    }

    [Then("a commit audit exists")]
    public void ThenAuditExists()
    {
        Assert.NotNull(_repo!.GetLastAudit(nameof(YourEntity), "1"));
    }

    [Then("no SaveCommitFault is published")]
    public async Task ThenNoFault()
    {
        Assert.False(await _harness!.Published.Any<SaveCommitFault<YourEntity>>());
        await _harness!.Stop();
    }
}
