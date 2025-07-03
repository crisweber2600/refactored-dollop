using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExampleLib.Domain;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Reqnroll;
using Sample.EventDrivenDemo.ServiceA;
using Sample.EventDrivenDemo.ServiceB;
using Sample.EventDrivenDemo.ServiceB.Data;
using Sample.EventDrivenDemo.Shared;
using Xunit;

namespace MetricsPipeline.Tests.Steps;

[Binding]
public class EventDrivenSteps
{
    private ServiceProvider _provider = null!;
    private OrdersDbContext? _db;
    private List<SaveAudit>? _audits;

    [Given("commit consumer is enabled")]
    public void GivenCommitConsumer()
    {
        _provider = Startup.Configure();
        _db = new OrdersDbContext(new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    }

    [When("the demo runs with (\\d+) orders")]
    public async Task WhenDemoRuns(int count)
    {
        if (_provider == null) _provider = Startup.Configure();
        var bus = _provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();
        try
        {
            var client = new Client(_provider);
            _audits = await client.RunDemoAsync(count);
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Then("both valid and invalid audits should exist")]
    public void ThenAuditsExist()
    {
        _audits.Should().NotBeNull();
        _audits!.Any(a => a.Validated).Should().BeTrue();
        _audits.Any(a => !a.Validated).Should().BeTrue();
    }

    [Then("only valid orders should be stored")]
    public void ThenOnlyValidOrdersStored()
    {
        _audits.Should().NotBeNull();
        _audits!.All(a => a.Validated).Should().BeTrue();
    }
}
