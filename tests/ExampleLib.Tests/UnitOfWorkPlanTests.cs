using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq.Expressions;
using System.Threading;

namespace ExampleLib.Tests;

public class UnitOfWorkPlanTests
{
    [Fact]
    public async Task SaveChangesWithPlanAsync_UsesPlan()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("plan-test")
            .Options;
        using var context = new YourDbContext(options);
        var mock = new Mock<IValidationService>();
        mock.Setup(s => s.ComputeAsync<YourEntity>(It.IsAny<Expression<Func<YourEntity, double>>>(), ValidationStrategy.Sum, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        var store = new InMemoryValidationPlanProvider();
        store.AddPlan(new ValidationPlan<YourEntity>(e => e.Id, ThresholdType.RawDifference, 1));
        var uow = new UnitOfWork<YourDbContext>(context, mock.Object, store);
        var repo = uow.Repository<YourEntity>();

        await repo.AddAsync(new YourEntity { Name = "Plan" });
        await uow.SaveChangesWithPlanAsync<YourEntity>();

        var entity = await context.YourEntities.FirstAsync();
        Assert.True(entity.Validated);
    }
}
