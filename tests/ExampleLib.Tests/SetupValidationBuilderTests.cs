using ExampleData;
using ExampleLib.Infrastructure;
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
        var builder = new SetupValidationBuilder()
            .UseMongo("mongodb://localhost:27017", "test");
        builder.Apply(services);
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IMongoDatabase>());
        Assert.IsType<MongoUnitOfWork>(provider.GetRequiredService<IUnitOfWork>());
    }
}
