using ExampleData;
using ExampleLib.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Reqnroll;
using Xunit;

namespace ExampleLib.BDDTests;

[Binding]
public class SetupValidationBuilderSteps
{
    private SetupValidationBuilder? _builder;
    private ServiceCollection? _services;
    private ServiceProvider? _provider;

    [Given("a new setup builder")]
    public void GivenNewBuilder()
    {
        _builder = new SetupValidationBuilder();
        _services = new ServiceCollection();
    }

    [When("UseSqlServer is called")]
    public void WhenUseSqlServer()
    {
        _builder!.UseSqlServer<YourDbContext>("DataSource=:memory:");
    }

    [When("UseMongo is called")]
    public void WhenUseMongo()
    {
        _builder!.UseMongo("mongodb://localhost:27017", "bdd");
    }

    [When("Apply is invoked")]
    public void WhenApplyInvoked()
    {
        _builder!.Apply(_services!);
        _provider = _services!.BuildServiceProvider();
    }

    [Then("a unit of work can be resolved")]
    public void ThenUnitOfWorkResolvable()
    {
        Assert.NotNull(_provider!.GetService<IUnitOfWork>());
    }

    [Then("a Mongo unit of work can be resolved")]
    public void ThenMongoUnitOfWorkResolvable()
    {
        Assert.IsType<MongoUnitOfWork>(_provider!.GetRequiredService<IUnitOfWork>());
    }
}
