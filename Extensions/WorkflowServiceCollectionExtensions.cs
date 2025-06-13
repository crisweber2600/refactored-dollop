using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using refactored_dollop.Data;
using refactored_dollop.Repositories;

namespace refactored_dollop.Extensions;

public static class WorkflowServiceCollectionExtensions
{
    public static IServiceCollection ConfigureWorkflow(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<WorkflowContext>(o => o.UseSqlServer(config.GetConnectionString("Default")));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            x.UsingInMemory();
        });

        return services;
    }
}
