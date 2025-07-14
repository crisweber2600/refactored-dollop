using ExampleData;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Xunit;

namespace ExampleLib.BDDTests;

[Binding]
public class SqlServerConfigurationSteps
{
    private ServiceCollection? _services;
    private ServiceProvider? _provider;

    [Given("a new service collection")]
    public void GivenNewServiceCollection()
    {
        _services = new ServiceCollection();
    }

    [When("AddExampleDataSqlServer is invoked")]
    public void WhenAddExampleDataSqlServerInvoked()
    {
        _services!.AddExampleDataSqlServer("Server=(localdb)\\mssqllocaldb;Database=BDD;Trusted_Connection=True;");
        _provider = _services!.BuildServiceProvider();
    }

    [Then("the DbContext should use SqlServer")]
    public void ThenDbContextUsesSqlServer()
    {
        var ctx = _provider!.GetRequiredService<YourDbContext>();
        var ext = ctx.GetService<IDbContextOptions>()
            .FindExtension<Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal.SqlServerOptionsExtension>();
        Assert.NotNull(ext);
    }
}
