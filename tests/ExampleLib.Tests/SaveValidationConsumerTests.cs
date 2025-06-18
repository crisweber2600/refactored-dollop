using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using MassTransit;
using MassTransit.Testing;

namespace ExampleLib.Tests;

public class SaveValidationConsumerTests
{
    [Fact]
    public async Task Consume_AddsAuditAndPublishesEvent()
    {
        var planStore = new InMemorySummarisationPlanStore();
        var plan = new SummarisationPlan<YourEntity>(e => e.Id, ThresholdType.RawDifference, 5);
        planStore.AddPlan(plan);
        var auditRepo = new InMemorySaveAuditRepository();
        var validator = new SummarisationValidator<YourEntity>();

        using var harness = new InMemoryTestHarness();
        harness.Consumer(() => new SaveValidationConsumer<YourEntity>(planStore, auditRepo, validator));

        await harness.Start();
        try
        {
            var message = new SaveRequested<YourEntity>
            {
                AppName = "App",
                EntityType = nameof(YourEntity),
                EntityId = "1",
                Payload = new YourEntity { Id = 10 }
            };
            await harness.InputQueueSendEndpoint.Send(message);

            Assert.True(await harness.Published.Any<SaveValidated<YourEntity>>());
            var audit = auditRepo.GetLastAudit(nameof(YourEntity), "1");
            Assert.NotNull(audit);
            Assert.True(audit.Validated);
        }
        finally
        {
            await harness.Stop();
        }
    }
}
