using ExampleLib.BDD;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Plan2RepositoryUoW.Application.Services;
using Plan2RepositoryUoW.Infrastructure;
using Plan2RepositoryUoW.Domain.Entities;

namespace Plan2RepositoryUoW.Tests;

[Binding]
public class Plan2CrudSteps
{
    private IEntityCrudService? _svc;
    private Guid _id;
    private YourEntity? _result;

    [Given("a plan2 service")]
    public void GivenService()
    {
        var sp = new ServiceCollection().AddPlan2Services().BuildServiceProvider();
        _svc = sp.GetRequiredService<IEntityCrudService>();
    }

    [When("I create an entity with score (.*)")]
    public async Task WhenCreate(double score)
    {
        _id = await _svc!.CreateAsync("demo", score);
        _result = await _svc.GetAsync(_id, true);
    }

    [Then("the entity should be validated")]
    public void ThenValidated()
    {
        _result!.Validated.Should().BeTrue();
    }

    [Then("the entity should be rejected")]
    public void ThenRejected()
    {
        _result!.Validated.Should().BeFalse();
    }
}
