using ExampleLib.Infrastructure;
using ExampleData;
using Microsoft.Extensions.DependencyInjection;
using Plan2RepositoryUoW.Application.Services;
using Plan2RepositoryUoW.Infrastructure.Data;
using Serilog;

namespace Plan2RepositoryUoW.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlan2Services(this IServiceCollection services)
    {
        services.SetupDatabase<Plan2RepositoryUoW.Infrastructure.Data.YourDbContext>("DataSource=:memory:");
        services.AddLogging(lb => lb.AddSerilog());
        services.AddScoped<IEntityCrudService, EntityCrudService>();
        return services;
    }
}
