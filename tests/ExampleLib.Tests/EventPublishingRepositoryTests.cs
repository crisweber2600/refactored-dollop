using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using MassTransit.Testing;
using Xunit;

namespace ExampleLib.Tests;

public class EventPublishingRepositoryTests
{
    [Fact]
    public async Task SaveAsync_PublishesEventWithEntityId()
    {
        using var harness = new InMemoryTestHarness();

        await harness.Start();
        var repo = new EventPublishingRepository<YourEntity>(harness.Bus);
        try
        {
            var entity = new YourEntity { Id = 1, Name = "One" };
            await repo.SaveAsync("App", entity);

            Assert.True(await harness.Published.Any<SaveRequested<YourEntity>>());
        }
        finally
        {
            await harness.Stop();
        }
    }

    private class NoIdEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task SaveAsync_GeneratesIdWhenMissing()
    {
        using var harness = new InMemoryTestHarness();

        await harness.Start();
        var repo = new EventPublishingRepository<NoIdEntity>(harness.Bus);
        try
        {
            var entity = new NoIdEntity { Name = "Test" };
            await repo.SaveAsync("App", entity);

            Assert.True(await harness.Published.Any<SaveRequested<NoIdEntity>>());
        }
        finally
        {
            await harness.Stop();
        }
    }
}
