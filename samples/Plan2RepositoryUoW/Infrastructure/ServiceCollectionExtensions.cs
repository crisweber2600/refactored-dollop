using ExampleLib.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Plan2RepositoryUoW.Application.Services;
using Plan2RepositoryUoW.Infrastructure.Data;
using Serilog;

namespace Plan2RepositoryUoW.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlan2Services(this IServiceCollection services)
    {
        services.SetupDatabase<YourDbContext>("DataSource=:memory:");
        services.AddLogging(lb => lb.AddSerilog());
        services.AddScoped<IEntityCrudService, EntityCrudService>();
        return services;
    }
}
