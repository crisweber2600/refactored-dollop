using ExampleData;
using Microsoft.EntityFrameworkCore;

namespace ExampleLib.Tests;

public class RepositoryTests
{
    [Fact]
    public async Task AddAndCount_Works()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("repo-test")
            .Options;
        using var context = new YourDbContext(options);
        var repo = new EfGenericRepository<YourEntity>(context);

        await repo.AddAsync(new YourEntity { Name = "One", Validated = true });
        await context.SaveChangesAsync();

        Assert.Equal(1, await repo.CountAsync());
    }

    [Fact]
    public async Task Delete_UnvalidatesEntity()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("delete-test")
            .Options;
        using var context = new YourDbContext(options);
        var repo = new EfGenericRepository<YourEntity>(context);

        var entity = new YourEntity { Name = "Two" };
        await repo.AddAsync(entity);
        await context.SaveChangesAsync();

        await repo.DeleteAsync(entity);

        Assert.False(entity.Validated);
    }

    [Fact]
    public async Task HardDelete_RemovesEntity()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("hard-delete-test")
            .Options;
        using var context = new YourDbContext(options);
        var repo = new EfGenericRepository<YourEntity>(context);

        var entity = new YourEntity { Name = "Three", Validated = true };
        await repo.AddAsync(entity);
        await context.SaveChangesAsync();

        await repo.DeleteAsync(entity, hardDelete: true);

        Assert.Equal(0, await repo.CountAsync());
    }

    [Fact]
    public async Task GetById_CanIncludeDeleted()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("include-deleted-test")
            .Options;
        using var context = new YourDbContext(options);
        var repo = new EfGenericRepository<YourEntity>(context);

        var entity = new YourEntity { Name = "Four", Validated = true };
        await repo.AddAsync(entity);
        await context.SaveChangesAsync();

        await repo.DeleteAsync(entity);

        var result = await repo.GetByIdAsync(entity.Id, includeDeleted: true);

        Assert.NotNull(result);
    }
}
