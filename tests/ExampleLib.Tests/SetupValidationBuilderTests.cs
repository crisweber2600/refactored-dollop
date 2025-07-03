using ExampleData;
using ExampleLib.Infrastructure;
using ExampleLib.Domain;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Xunit;

namespace ExampleLib.Tests;

public class SetupValidationBuilderTests
{
    [Fact]
    public void UseSqlServer_ConfiguresUnitOfWork()
    {
        var services = new ServiceCollection();
        var store = new InMemorySummarisationPlanStore();
        store.AddPlan(new SummarisationPlan<YourEntity>(e => e.Id, ThresholdType.RawDifference, 1));
        services.AddSingleton<ISummarisationPlanStore>(store);
        var builder = new SetupValidationBuilder()
            .UseSqlServer<YourDbContext>("DataSource=:memory:");
        builder.Apply(services);
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IUnitOfWork>());
    }

    [Fact]
    public void UseMongo_ConfiguresMongoServices()
    {
        var services = new ServiceCollection();
        var store = new InMemorySummarisationPlanStore();
        store.AddPlan(new SummarisationPlan<YourEntity>(e => e.Id, ThresholdType.RawDifference, 1));
        services.AddSingleton<ISummarisationPlanStore>(store);
        var builder = new SetupValidationBuilder()
            .UseMongo("mongodb://localhost:27017", "test");
        builder.Apply(services);
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IMongoDatabase>());
        Assert.IsType<MongoUnitOfWork>(provider.GetRequiredService<IUnitOfWork>());
    }
}
