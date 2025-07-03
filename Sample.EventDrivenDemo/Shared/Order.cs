namespace Sample.EventDrivenDemo.Shared;

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public List<decimal> LineAmounts { get; set; } = new();
    public decimal TotalAmount => LineAmounts.Sum();
}