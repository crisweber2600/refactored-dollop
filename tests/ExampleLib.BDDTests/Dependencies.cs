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
        services.AddDbContext<YourDbContext, TestDbContext>(opts =>
            opts.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddScoped(typeof(IGenericRepository<>), typeof(EfGenericRepository<>));
        services.AddScoped<IValidationService, ValidationService>();
        services.AddScoped<IUnitOfWork, UnitOfWork<YourDbContext>>();
        services.AddSingleton(typeof(ISummarisationValidator<>), typeof(SummarisationValidator<>));
        services.AddSingleton<ISummarisationPlanStore, InMemorySummarisationPlanStore>();

        var repo = new InMemorySaveAuditRepository();
        services.AddSingleton<ISaveAuditRepository>(repo);

        services.AddValidatorService()
                .AddValidatorRule<Foo>(new FooValidator(repo).Validate);

        return services;
    }
}
