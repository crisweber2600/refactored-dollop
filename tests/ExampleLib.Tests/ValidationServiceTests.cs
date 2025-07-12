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

    [Fact]
    public async Task ComputeAsync_Average_Works()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("avg-test")
            .Options;
        using var context = new YourDbContext(options);
        context.YourEntities.AddRange(new YourEntity { Id = 2 }, new YourEntity { Id = 4 });
        await context.SaveChangesAsync();

        var service = new ValidationService(context);
        var avg = await service.ComputeAsync<YourEntity>(e => e.Id, ValidationStrategy.Average);

        Assert.Equal(3, avg);
    }

    [Fact]
    public async Task ComputeAsync_Count_Works()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("count-test")
            .Options;
        using var context = new YourDbContext(options);
        context.YourEntities.AddRange(new YourEntity { Id = 1 }, new YourEntity { Id = 2 });
        await context.SaveChangesAsync();

        var service = new ValidationService(context);
        var count = await service.ComputeAsync<YourEntity>(e => e.Id, ValidationStrategy.Count);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ComputeAsync_Variance_Works()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("var-test")
            .Options;
        using var context = new YourDbContext(options);
        context.YourEntities.AddRange(
            new YourEntity { Id = 1 },
            new YourEntity { Id = 3 },
            new YourEntity { Id = 5 }
        );
        await context.SaveChangesAsync();

        var service = new ValidationService(context);
        var variance = await service.ComputeAsync<YourEntity>(e => e.Id, ValidationStrategy.Variance);

        Assert.Equal(8/3d, variance, 3);
    }
}
