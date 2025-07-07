using ExampleData;

namespace Sample.EventDrivenDemo.Shared;

public sealed class Order : IValidatable, IBaseEntity, IRootEntity
{
    public int Id { get; set; } = Random.Shared.Next(1000, 9999);
    public List<decimal> LineAmounts { get; set; } = new();
    public decimal TotalAmount => LineAmounts.Sum();
    public bool Validated { get; set; } = true;
}
