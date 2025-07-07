<<<<<< ic50pi-codex/plan-event-driven-crud-demo-implementation
namespace Sample.EventDrivenDemo.Shared;

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public List<decimal> LineAmounts { get; set; } = new();
    public decimal TotalAmount => LineAmounts.Sum();
}
======
using ExampleData;

namespace Sample.EventDrivenDemo.Shared;

public sealed class Order : IValidatable, IBaseEntity, IRootEntity
{
    public int Id { get; set; } = Random.Shared.Next(1000, 9999);
    public List<decimal> LineAmounts { get; set; } = new();
    public decimal TotalAmount => LineAmounts.Sum();
    public bool Validated { get; set; } = true;
}
>>>>>> main
