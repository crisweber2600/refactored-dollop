using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace ExampleLib.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddSaveValidation_WiresEndToEnd()
    {
        var services = new ServiceCollection();
        services.AddSaveValidation<YourEntity>(e => e.Id);
        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();
        try
        {
            var repo = provider.GetRequiredService<IEntityRepository<YourEntity>>();
            await repo.SaveAsync(new YourEntity { Id = 1 });
            await Task.Delay(200);
            var audits = provider.GetRequiredService<ISaveAuditRepository>();
            var audit = audits.GetLastAudit(nameof(YourEntity), "1");
            Assert.NotNull(audit);
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Fact]
    public void SetupValidation_ExecutesBuilder()
    {
        var services = new ServiceCollection();
        services.SetupValidation(b => b.UseSqlServer<YourDbContext>("DataSource=:memory:"));
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IUnitOfWork>());
    }

    [Fact]
    public void SetupDatabase_RegistersDbContext()
    {
        var services = new ServiceCollection();
        services.SetupDatabase<YourDbContext>("DataSource=:memory:");
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IUnitOfWork>());
    }

    [Fact]
    public void SetupMongoDatabase_RegistersMongoServices()
    {
        var services = new ServiceCollection();
        services.SetupMongoDatabase("mongodb://localhost:27017", "test");
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IMongoDatabase>());
        Assert.IsType<MongoUnitOfWork>(provider.GetRequiredService<IUnitOfWork>());
        Assert.NotNull(provider.GetService(typeof(IGenericRepository<YourEntity>)));
    }
}
