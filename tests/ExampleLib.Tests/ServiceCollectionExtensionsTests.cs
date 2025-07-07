using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

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

    [Fact(Skip="Requires SQL Server provider")]
    public async Task AddSetupValidation_WiresEndToEnd()
    {
        var services = new ServiceCollection();
        services.AddSetupValidation<YourEntity>(
            b => b.UseSqlServer<YourDbContext>("DataSource=:memory:"),
            e => e.Id);
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
        var store = new InMemorySummarisationPlanStore();
        store.AddPlan(new SummarisationPlan<YourEntity>(e => e.Id, ThresholdType.RawDifference, 1));
        services.AddSingleton<ISummarisationPlanStore>(store);
        services.SetupValidation(b => b.UseSqlServer<YourDbContext>("DataSource=:memory:"));
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IUnitOfWork>());
    }

    [Fact]
    public void SetupDatabase_RegistersDbContext()
    {
        var services = new ServiceCollection();
        var store = new InMemorySummarisationPlanStore();
        store.AddPlan(new SummarisationPlan<YourEntity>(e => e.Id, ThresholdType.RawDifference, 1));
        services.AddSingleton<ISummarisationPlanStore>(store);
        services.SetupDatabase<YourDbContext>("DataSource=:memory:");
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IUnitOfWork>());
    }

    [Fact]
    public void SetupMongoDatabase_RegistersMongoServices()
    {
        var services = new ServiceCollection();
        var store = new InMemorySummarisationPlanStore();
        store.AddPlan(new SummarisationPlan<YourEntity>(e => e.Id, ThresholdType.RawDifference, 1));
        services.AddSingleton<ISummarisationPlanStore>(store);
        services.SetupMongoDatabase("mongodb://localhost:27017", "test");
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IMongoDatabase>());
        Assert.IsType<MongoUnitOfWork>(provider.GetRequiredService<IUnitOfWork>());
        Assert.NotNull(provider.GetService(typeof(IGenericRepository<YourEntity>)));
    }

    [Fact]
    public void AddValidatorService_RegistersManualValidator()
    {
        var services = new ServiceCollection();
        services.AddValidatorService();
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IManualValidatorService>());
        Assert.NotNull(provider.GetService<IDictionary<Type, List<Func<object, bool>>>>());
    }

    [Fact]
    public void AddValidatorRule_StoresRule()
    {
        var services = new ServiceCollection();
        services.AddValidatorService();
        services.AddValidatorRule<YourEntity>(_ => true);
        var provider = services.BuildServiceProvider();
        var dict = provider.GetRequiredService<IDictionary<Type, List<Func<object, bool>>>>();
        Assert.True(dict.ContainsKey(typeof(YourEntity)));
        Assert.Single(dict[typeof(YourEntity)]);
    }
}
