using ExampleData;
using Microsoft.EntityFrameworkCore;

namespace ExampleLib.Tests;

public class ValidationServiceTests
{
    [Fact]
    public async Task ComputeAsync_Sum_Works()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("sum-test")
            .Options;
        using var context = new YourDbContext(options);
        context.YourEntities.AddRange(new YourEntity { Name = "A", Id = 1, Validated = true },
            new YourEntity { Name = "B", Id = 2, Validated = true });
        await context.SaveChangesAsync();

        var service = new ValidationService(context);
        var sum = await service.ComputeAsync<YourEntity>(e => e.Id, ValidationStrategy.Sum);

        Assert.Equal(3, sum);
    }
}
