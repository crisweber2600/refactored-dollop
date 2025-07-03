using ExampleLib.Infrastructure;
using ExampleLib.Domain;
using Microsoft.Extensions.DependencyInjection;
using Sample.EventDrivenDemo.Shared;

namespace Sample.EventDrivenDemo.ServiceB;

public static class Startup
{
    public static ServiceProvider Configure()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSaveValidation<Order>(
            o => o.TotalAmount,
            ThresholdType.PercentChange,
            0.5m);
        return services.BuildServiceProvider();
    }
}