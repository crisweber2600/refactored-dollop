using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

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
    public void SetupValidation_IsAlias()
    {
        var services = new ServiceCollection();
        services.SetupValidation<YourEntity>(e => e.Id);
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IEntityRepository<YourEntity>>());
    }

    [Fact]
    public void SetupDatabase_RegistersDbContext()
    {
        var services = new ServiceCollection();
        services.SetupDatabase<YourDbContext>("DataSource=:memory:");
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IUnitOfWork>());
    }
}
