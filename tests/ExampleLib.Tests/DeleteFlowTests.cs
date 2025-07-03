using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Messages;
using MassTransit.Testing;
using Moq;
using Xunit;

namespace ExampleLib.Tests;

public class DeleteFlowTests
{
    [Fact]
    public async Task ValidationConsumer_PublishesValidatedEvent()
    {
        var validator = new Mock<IManualValidatorService>();
        validator.Setup(v => v.Validate(It.IsAny<YourEntity>())).Returns(true);

        using var harness = new InMemoryTestHarness();
        harness.Consumer(() => new DeleteValidationConsumer<YourEntity>(validator.Object));

        await harness.Start();
        try
        {
            var message = new DeleteRequested<YourEntity>
            {
                AppName = "App",
                EntityType = nameof(YourEntity),
                EntityId = "1",
                Payload = new YourEntity { Id = 1 }
            };
            await harness.InputQueueSendEndpoint.Send(message);

            Assert.True(await harness.Published.Any<DeleteValidated<YourEntity>>());
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task CommitConsumer_PublishesCommittedEventWhenValidated()
    {
        using var harness = new InMemoryTestHarness();
        harness.Consumer(() => new DeleteCommitConsumer<YourEntity>());

        await harness.Start();
        try
        {
            var message = new DeleteValidated<YourEntity>("App", nameof(YourEntity), "1", new YourEntity { Id = 1 }, true);
            await harness.InputQueueSendEndpoint.Send(message);

            Assert.True(await harness.Published.Any<DeleteCommitted<YourEntity>>());
        }
        finally
        {
            await harness.Stop();
        }
    }
}
