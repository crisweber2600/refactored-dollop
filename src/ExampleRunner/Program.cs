using System;
using System.Linq;
using System.Threading.Tasks;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

// Sample entity class
public class Order
{
    public string Id { get; set; } = string.Empty;
    public decimal[] LineAmounts { get; set; } = Array.Empty<decimal>();
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddSaveValidation<Order>(o => o.LineAmounts.Sum(), ThresholdType.PercentChange, 0.50m);
        var provider = services.BuildServiceProvider();

        var busControl = provider.GetRequiredService<IBusControl>();
        await busControl.StartAsync();
        try
        {
            var repository = provider.GetRequiredService<IEntityRepository<Order>>();
            
            var order = new Order { Id = "ORDER123", LineAmounts = new decimal[] { 100m, 50m } };
            Console.WriteLine($"Saving Order {order.Id} with total = {order.LineAmounts.Sum()}");
            await repository.SaveAsync(order);
            await Task.Delay(500);

            var auditRepo = provider.GetRequiredService<ISaveAuditRepository>();
            var audit = auditRepo.GetLastAudit(nameof(Order), "ORDER123");
            if (audit != null)
                Console.WriteLine($"Last audit for Order {audit.EntityId}: MetricValue={audit.MetricValue}, Validated={audit.Validated}");

            order.LineAmounts = new decimal[] { 300m, 100m };
            Console.WriteLine($"Saving Order {order.Id} with new total = {order.LineAmounts.Sum()}");
            await repository.SaveAsync(order);
            await Task.Delay(500);

            audit = auditRepo.GetLastAudit(nameof(Order), "ORDER123");
            if (audit != null)
                Console.WriteLine($"Last audit for Order {audit.EntityId}: MetricValue={audit.MetricValue}, Validated={audit.Validated}");
        }
        finally
        {
            await busControl.StopAsync();
        }
    }
}
