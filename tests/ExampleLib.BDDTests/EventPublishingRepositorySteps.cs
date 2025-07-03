using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Xunit;
using MassTransit.Testing;
using Reqnroll;

namespace ExampleLib.BDDTests;

[Binding]
public class EventPublishingRepositorySteps
{
    private InMemoryTestHarness? _harness;
    private EventPublishingRepository<YourEntity>? _repo;

    [Given("an event publishing repository")]
    public void GivenRepository()
    {
        _harness = new InMemoryTestHarness();
        _repo = new EventPublishingRepository<YourEntity>(_harness.Bus);
        _harness.Start().Wait();
    }

    [When("the entity is saved")]
    public async Task WhenEntitySaved()
    {
        var entity = new YourEntity { Id = 1, Name = "Test" };
        if (_repo != null)
            await _repo.SaveAsync(entity);
    }

    [Then("a SaveRequested event should be published")]
    public async Task ThenEventPublished()
    {
        if (_harness == null) throw new Exception("harness not started");
        Assert.True(await _harness.Published.Any<SaveRequested<YourEntity>>());
        await _harness.Stop();
    }
}
