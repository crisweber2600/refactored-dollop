using MassTransit;
using ExampleLib.Domain;
using Sample.EventDrivenDemo.Shared;
using Sample.EventDrivenDemo.ServiceB.Data;

namespace Sample.EventDrivenDemo.ServiceB;

public class SaveCommitConsumer<T> : IConsumer<SaveValidated<T>> where T : class
{
    private readonly OrdersDbContext _db;
    public SaveCommitConsumer(OrdersDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<SaveValidated<T>> ctx)
    {
        if (ctx.Message.Validated && ctx.Message.Payload != null)
        {
            _db.Add(ctx.Message.Payload);
            await _db.SaveChangesAsync();
        }
    }
}
