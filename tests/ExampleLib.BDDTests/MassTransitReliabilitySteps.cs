using ExampleLib.Infrastructure;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Serilog;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ExampleLib.BDDTests;

[Binding]
public class MassTransitReliabilitySteps
{
    private InMemoryTestHarness? _harness;
    private int _attempts;

    [Given("a reliability configured service collection")]
    public async Task GivenReliabilitySetup()
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

        var services = new ServiceCollection();
        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<TestConsumer>();
            x.UsingInMemory((ctx,cfg) =>
            {
                cfg.UseMessageRetry(r => r.Immediate(2));
                cfg.ReceiveEndpoint("reliability", e =>
                {
                    e.UseInMemoryOutbox();
                    e.ConfigureConsumer<TestConsumer>(ctx);
                });
            });
        });
        services.AddSingleton(this);

        var provider = services.BuildServiceProvider(true);
        _harness = provider.GetRequiredService<InMemoryTestHarness>();
        await _harness.Start();
    }

    [When("a failing message is published")]
    public async Task WhenMessagePublished()
    {
        await _harness!.Bus.Publish(new PingMessage());
    }

    [Then("the consumer should retry")]
    public void ThenRetry()
    {
        Assert.Equal(2, _attempts);
    }

    private class TestConsumer : IConsumer<PingMessage>
    {
        private readonly MassTransitReliabilitySteps _parent;
        public TestConsumer(MassTransitReliabilitySteps parent) => _parent = parent;
        public Task Consume(ConsumeContext<PingMessage> context)
        {
            _parent._attempts++;
            if (_parent._attempts < 2)
                throw new InvalidOperationException("fail");
            return Task.CompletedTask;
        }
    }

    private record PingMessage;
}
