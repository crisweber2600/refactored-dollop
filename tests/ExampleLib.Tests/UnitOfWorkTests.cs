using ExampleData;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq.Expressions;
using System.Threading;

namespace ExampleLib.Tests;

public class UnitOfWorkTests
{
    [Fact]
    public async Task SaveChangesAsync_ValidatesEntities()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("uow-validate")
            .Options;
        using var context = new YourDbContext(options);
        var service = new ValidationService(context);
        var uow = new UnitOfWork<YourDbContext>(context, service);
        var repo = uow.Repository<YourEntity>();

        context.YourEntities.Add(new YourEntity { Name = "Existing", Validated = true });
        await context.SaveChangesAsync();

        await repo.AddAsync(new YourEntity { Name = "Test" });
        await uow.SaveChangesAsync<YourEntity>(e => e.Id, ValidationStrategy.Count, 1);

        var entity = await context.YourEntities.IgnoreQueryFilters().FirstAsync();
        Assert.True(entity.Validated);
    }

    [Fact]
    public async Task SaveChangesAsync_MarksValidated_WhenThresholdMet()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("uow-valid-mock")
            .Options;
        using var context = new YourDbContext(options);
        var mock = new Mock<IValidationService>();
        mock.Setup(s => s.ComputeAsync<YourEntity>(It.IsAny<Expression<Func<YourEntity, double>>>(), ValidationStrategy.Count, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        var uow = new UnitOfWork<YourDbContext>(context, mock.Object);
        var repo = uow.Repository<YourEntity>();

        await repo.AddAsync(new YourEntity { Name = "Mock" });
        await uow.SaveChangesAsync<YourEntity>(e => e.Id, ValidationStrategy.Count, 1);

        var entity = await context.YourEntities.FirstAsync();
        Assert.True(entity.Validated);
    }

    [Fact]
    public async Task SaveChangesAsync_MarksUnvalidated_WhenThresholdNotMet()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("uow-invalid-mock")
            .Options;
        using var context = new YourDbContext(options);
        var mock = new Mock<IValidationService>();
        mock.Setup(s => s.ComputeAsync<YourEntity>(It.IsAny<Expression<Func<YourEntity, double>>>(), ValidationStrategy.Count, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var uow = new UnitOfWork<YourDbContext>(context, mock.Object);
        var repo = uow.Repository<YourEntity>();

        await repo.AddAsync(new YourEntity { Name = "Mock" });
        await uow.SaveChangesAsync<YourEntity>(e => e.Id, ValidationStrategy.Count, 1);

        var entity = await context.YourEntities.IgnoreQueryFilters().FirstAsync();
        Assert.False(entity.Validated);
    }

    [Fact]
    public async Task SaveChangesAsync_WritesNannyRecord()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("nanny-test")
            .Options;
        using var context = new YourDbContext(options);
        var service = new ValidationService(context);
        var uow = new UnitOfWork<YourDbContext>(context, service);
        var repo = uow.Repository<YourEntity>();

        await repo.AddAsync(new YourEntity { Name = "Nanny" });
        await uow.SaveChangesAsync<YourEntity>(e => e.Id, ValidationStrategy.Count, 0);

        var nanny = await context.Nannies.FirstAsync();
        Assert.Equal("YourEntity", nanny.Entity);
    }

    [Fact]
    public async Task SaveChangesAsync_RuleSet_AllRulesMustPass()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("ruleset-test")
            .Options;
        using var context = new YourDbContext(options);
        var mock = new Mock<IValidationService>();
        mock.Setup(s => s.ComputeAsync<YourEntity>(It.IsAny<Expression<Func<YourEntity, double>>>(), It.IsAny<ValidationStrategy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var uow = new UnitOfWork<YourDbContext>(context, mock.Object);
        var repo = uow.Repository<YourEntity>();

        await repo.AddAsync(new YourEntity { Name = "Rule" });
        var rules = new ValidationRuleSet<YourEntity>(e => e.Id,
            new ValidationRule(ValidationStrategy.Count, 1),
            new ValidationRule(ValidationStrategy.Sum, 1));
        await uow.SaveChangesAsync(rules);

        var entity = await context.YourEntities.FirstAsync();
        Assert.True(entity.Validated);
    }
}
