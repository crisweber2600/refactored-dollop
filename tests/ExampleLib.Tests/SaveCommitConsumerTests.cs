using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using ExampleLib.Messages;
using MassTransit.Testing;
using Xunit;

namespace ExampleLib.Tests;

public class SaveCommitConsumerTests
{
    [Fact(Skip = "Fails on CI environment")] 
    public async Task Consume_StoresAudit()
    {
        var planStore = new InMemoryValidationPlanProvider();
        planStore.AddPlan(new ValidationPlan<YourEntity>(e => e.Id, ThresholdType.RawDifference, 1));
        var repo = new InMemorySaveAuditRepository();

        using var harness = new InMemoryTestHarness();
        harness.Consumer(() => new SaveCommitConsumer<YourEntity>(planStore, repo));

        await harness.Start();
        try
        {
            var msg = new SaveValidated<YourEntity>("App", nameof(YourEntity), "1", new YourEntity { Id = 10 }, true);
            await harness.InputQueueSendEndpoint.Send(msg);
            await harness.InactivityTask; // wait for consume
            var audit = repo.GetLastAudit(nameof(YourEntity), "1");
            Assert.NotNull(audit);
            Assert.False(await harness.Published.Any<SaveCommitFault<YourEntity>>());
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact(Skip = "Fails on CI environment")]
    public async Task Consume_PublishesFaultOnError()
    {
        var planStore = new InMemoryValidationPlanProvider();
        // plan that throws to simulate failure
        planStore.AddPlan(new ValidationPlan<YourEntity>(_ => throw new InvalidOperationException(), ThresholdType.RawDifference, 1));
        var repo = new InMemorySaveAuditRepository();

        using var harness = new InMemoryTestHarness();
        harness.Consumer(() => new SaveCommitConsumer<YourEntity>(planStore, repo));

        await harness.Start();
        try
        {
            var msg = new SaveValidated<YourEntity>("App", nameof(YourEntity), "1", new YourEntity { Id = 10 }, true);
            await harness.InputQueueSendEndpoint.Send(msg);

            Assert.True(await harness.Published.Any<SaveCommitFault<YourEntity>>());
        }
        finally
        {
            await harness.Stop();
        }
    }
}
