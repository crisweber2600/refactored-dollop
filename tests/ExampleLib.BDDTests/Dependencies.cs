using ExampleData;
using ExampleLib;
using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Reqnroll.Microsoft.Extensions.DependencyInjection;

namespace ExampleLib.BDDTests;

public static class Dependencies
{
    [ScenarioDependencies]
    public static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.SetupInMemoryDatabase<TestDbContext>();
        services.AddSingleton<ISaveAuditRepository, InMemorySaveAuditRepository>();
        return services;
    }
}
