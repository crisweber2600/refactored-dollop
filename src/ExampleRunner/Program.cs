using System;
using System.Linq;
using System.Threading.Tasks;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using MassTransit;

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
        var planStore = new InMemorySummarisationPlanStore();
        var auditRepo = new InMemorySaveAuditRepository();

        // summarise order total and allow 50% increase between saves
        planStore.AddPlan(new SummarisationPlan<Order>(o => o.LineAmounts.Sum(), ThresholdType.PercentChange, 0.50m));

        var busControl = Bus.Factory.CreateUsingInMemory(cfg =>
        {
            cfg.ReceiveEndpoint("save_requests_queue", e =>
            {
                e.Consumer(() => new SaveValidationConsumer<Order>(planStore, auditRepo, new SummarisationValidator<Order>()));
            });
        });

        await busControl.StartAsync();
        try
        {
            var repository = new EventPublishingRepository<Order>(busControl);

            var order = new Order { Id = "ORDER123", LineAmounts = new decimal[] { 100m, 50m } };
            Console.WriteLine($"Saving Order {order.Id} with total = {order.LineAmounts.Sum()}");
            await repository.SaveAsync("MyApp", order);
            await Task.Delay(500);

            var audit = auditRepo.GetLastAudit(nameof(Order), "ORDER123");
            if (audit != null)
                Console.WriteLine($"Last audit for Order {audit.EntityId}: MetricValue={audit.MetricValue}, Validated={audit.Validated}");

            order.LineAmounts = new decimal[] { 300m, 100m };
            Console.WriteLine($"Saving Order {order.Id} with new total = {order.LineAmounts.Sum()}");
            await repository.SaveAsync("MyApp", order);
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
