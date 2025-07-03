using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExampleLib.Domain;
using Microsoft.Extensions.DependencyInjection;
using Sample.EventDrivenDemo.Shared;

namespace Sample.EventDrivenDemo.ServiceA;

public class Client
{
    private readonly IEntityRepository<Order> _repo;
    private readonly ISaveAuditRepository _audits;

    public Client(IServiceProvider provider)
    {
        _repo = provider.GetRequiredService<IEntityRepository<Order>>();
        _audits = provider.GetRequiredService<ISaveAuditRepository>();
    }

    public async Task<List<SaveAudit>> RunDemoAsync(int count)
    {
        var audits = new List<SaveAudit>();
        for (int i = 0; i < count; i++)
        {
            var order = new Order { LineAmounts = new List<decimal> { 10m, 20m } };
            if (Random.Shared.NextDouble() < 0.2)
                order.LineAmounts[0] *= 10;
            await _repo.SaveAsync(order);
            await Task.Delay(100);
            var audit = _audits.GetLastAudit(nameof(Order), order.Id.ToString());
            if (audit != null) audits.Add(audit);
            Console.WriteLine($"Order {order.Id} valid={audit?.Validated}");
        }
        return audits;
    }
}
