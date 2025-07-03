using ExampleData;
using Microsoft.EntityFrameworkCore;
using Sample.EventDrivenDemo.Shared;

namespace Sample.EventDrivenDemo.ServiceB.Data;

public class OrdersDbContext : YourDbContext
{
    public OrdersDbContext(DbContextOptions<YourDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
}