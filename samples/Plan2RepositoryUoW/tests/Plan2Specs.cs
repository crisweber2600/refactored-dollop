using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Plan2RepositoryUoW.Application.Services;
using Plan2RepositoryUoW.Domain.Entities;
using Plan2RepositoryUoW.Infrastructure;
using Xunit;

namespace Plan2RepositoryUoW.Tests;

public class Plan2Specs
{
    private readonly ServiceProvider _provider;
    private readonly IEntityCrudService _svc;

    public Plan2Specs()
    {
        _provider = new ServiceCollection().AddPlan2Services().BuildServiceProvider();
        _svc = _provider.GetRequiredService<IEntityCrudService>();
    }

    [Fact]
    public async Task Create_Pass()
    {
        var id = await _svc.CreateAsync("ok", 80);
        var item = await _svc.GetAsync(id);
        item.Should().NotBeNull();
        item!.Validated.Should().BeTrue();
    }

    [Fact]
    public async Task Create_Fail()
    {
        var id = await _svc.CreateAsync("bad", 20);
        var item = await _svc.GetAsync(id);
        item.Should().BeNull();
        var deleted = await _svc.GetAsync(id, includeDeleted: true);
        deleted!.Validated.Should().BeFalse();
    }
}
