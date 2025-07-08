using ExampleData;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExampleLib.Tests;

public class ValidationFlowOptionsTests
{
    [Fact]
    public void Load_SingleObject_ParsesFlow()
    {
        string json = "{ \"Type\": \"ExampleData.YourEntity, ExampleData\", \"SaveValidation\": true }";
        var opts = ValidationFlowOptions.Load(json);
        Assert.Single(opts.Flows);
    }

    [Fact]
    public void Load_Array_ParsesFlows()
    {
        string json = "[ { \"Type\": \"ExampleData.YourEntity, ExampleData\", \"SaveValidation\": true }, { \"Type\": \"ExampleData.YourEntity, ExampleData\", \"DeleteValidation\": true } ]";
        var opts = ValidationFlowOptions.Load(json);
        Assert.Equal(2, opts.Flows.Count);
    }

    [Fact]
    public void AddValidationFlows_RegistersServices()
    {
        string json = "{ \"Type\": \"ExampleData.YourEntity, ExampleData\", \"SaveValidation\": true }";
        var opts = ValidationFlowOptions.Load(json);
        var services = new ServiceCollection();
        services.AddValidationFlows(opts);
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService(typeof(IEntityRepository<ExampleData.YourEntity>)));
    }
}
