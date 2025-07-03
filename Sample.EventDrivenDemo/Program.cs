using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Sample.EventDrivenDemo.ServiceA;
using Sample.EventDrivenDemo.ServiceB;

var provider = Startup.Configure();
var bus = provider.GetRequiredService<IBusControl>();
await bus.StartAsync();
try
{
    var client = new Client(provider);
    await client.RunDemoAsync(50);
}
finally
{
    await bus.StopAsync();
}