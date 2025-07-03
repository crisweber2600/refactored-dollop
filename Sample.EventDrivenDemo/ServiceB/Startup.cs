using ExampleLib.Infrastructure;
using ExampleLib.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sample.EventDrivenDemo.Shared;
using Sample.EventDrivenDemo.ServiceB.Data;

namespace Sample.EventDrivenDemo.ServiceB;

public static class Startup
{
    public static ServiceProvider Configure(bool useCommit = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSaveValidation<Order>(
            o => o.TotalAmount,
            ThresholdType.PercentChange,
            0.5m);

        if (useCommit)
        {
            services.AddDbContext<OrdersDbContext>(o =>
                o.UseInMemoryDatabase("orders"));

            services.AddMassTransit(x =>
            {
                x.AddConsumer<SaveCommitConsumer<Order>>();
                x.UsingInMemory((ctx, cfg) =>
                {
                    cfg.ReceiveEndpoint("save_commits_queue", e =>
                    {
                        e.ConfigureConsumer<SaveCommitConsumer<Order>>(ctx);
                    });
                });
            });
        }

        return services.BuildServiceProvider();
    }
}
