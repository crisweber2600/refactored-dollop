using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

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

    [Fact]
    public async Task AddMany_EF_AddsEntities()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("many-ef")
            .Options;
        using var context = new YourDbContext(options);
        var repo = new EfGenericRepository<YourEntity>(context);

        var entities = new[]
        {
            new YourEntity { Name = "Five", Validated = true },
            new YourEntity { Name = "Six", Validated = true }
        };
        await repo.AddManyAsync(entities);
        await context.SaveChangesAsync();

        Assert.Equal(2, await repo.CountAsync());
    }

    [Fact]
    public async Task Update_EF_PersistsChanges()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("update-ef")
            .Options;
        using var context = new YourDbContext(options);
        var repo = new EfGenericRepository<YourEntity>(context);

        var entity = new YourEntity { Name = "Old", Validated = true };
        await repo.AddAsync(entity);
        await context.SaveChangesAsync();

        entity.Name = "New";
        await repo.UpdateAsync(entity);
        await context.SaveChangesAsync();

        var reloaded = await repo.GetByIdAsync(entity.Id);
        Assert.Equal("New", reloaded?.Name);
    }

    [Fact]
    public async Task UpdateMany_EF_PersistsChanges()
    {
        var options = new DbContextOptionsBuilder<YourDbContext>()
            .UseInMemoryDatabase("update-many-ef")
            .Options;
        using var context = new YourDbContext(options);
        var repo = new EfGenericRepository<YourEntity>(context);

        var entities = new[]
        {
            new YourEntity { Name = "Old1", Validated = true },
            new YourEntity { Name = "Old2", Validated = true }
        };
        await repo.AddManyAsync(entities);
        await context.SaveChangesAsync();

        entities[0].Name = "New1";
        entities[1].Name = "New2";
        await repo.UpdateManyAsync(entities);
        await context.SaveChangesAsync();

        var list = await repo.GetAllAsync();
        Assert.Contains(list, e => e.Name == "New1");
        Assert.Contains(list, e => e.Name == "New2");
    }

    private class NoopValidationService : IValidationService
    {
        public Task<bool> ValidateAndSaveAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : IValidatable, IBaseEntity, IRootEntity
            => Task.FromResult(true);
    }

    private class FakeMongoCollection<T> : IMongoCollectionInterceptor<T>
        where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        public List<T> Items { get; } = new();

        public Task InsertOneAsync(T document, CancellationToken cancellationToken = default)
        {
            Items.Add(document);
            return Task.CompletedTask;
        }

        public Task<UpdateResult> UpdateOneAsync(FilterDefinition<T> filter, UpdateDefinition<T> update, CancellationToken cancellationToken = default)
            => Task.FromResult((UpdateResult)UpdateResult.Unacknowledged.Instance);

        public Task ReplaceOneAsync(FilterDefinition<T> filter, T replacement, CancellationToken cancellationToken = default)
        {
            var idx = Items.FindIndex(e => e.Id == replacement.Id);
            if (idx >= 0)
                Items[idx] = replacement;
            return Task.CompletedTask;
        }

        public Task<DeleteResult> DeleteOneAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default)
            => Task.FromResult((DeleteResult)DeleteResult.Unacknowledged.Instance);

        public IFindFluent<T, T> Find(FilterDefinition<T> filter) => throw new NotImplementedException();

        public Task<long> CountDocumentsAsync(FilterDefinition<T> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.LongCount(e => e.Validated));
    }

    [Fact]
    public async Task AddMany_Mongo_AddsEntities()
    {
        var collection = new FakeMongoCollection<YourEntity>();
        var repo = new MongoGenericRepository<YourEntity>(collection);

        var entities = new[]
        {
            new YourEntity { Name = "Seven", Validated = true },
            new YourEntity { Name = "Eight", Validated = true }
        };
        await repo.AddManyAsync(entities);

        Assert.Equal(2, collection.Items.Count);
    }

    [Fact]
    public async Task Update_Mongo_PersistsChanges()
    {
        var collection = new FakeMongoCollection<YourEntity>();
        var repo = new MongoGenericRepository<YourEntity>(collection);

        var entity = new YourEntity { Id = 1, Name = "Old", Validated = true };
        await repo.AddAsync(entity);

        entity.Name = "New";
        await repo.UpdateAsync(entity);

        Assert.Equal("New", collection.Items.First().Name);
    }

    [Fact]
    public async Task UpdateMany_Mongo_PersistsChanges()
    {
        var collection = new FakeMongoCollection<YourEntity>();
        var repo = new MongoGenericRepository<YourEntity>(collection);

        var entities = new[]
        {
            new YourEntity { Id = 1, Name = "Old1", Validated = true },
            new YourEntity { Id = 2, Name = "Old2", Validated = true }
        };
        await repo.AddManyAsync(entities);

        entities[0].Name = "New1";
        entities[1].Name = "New2";
        await repo.UpdateManyAsync(entities);

        Assert.Contains(collection.Items, e => e.Name == "New1");
        Assert.Contains(collection.Items, e => e.Name == "New2");
    }
}
