using Plan2RepositoryUoW.Application.Services;
using Plan2RepositoryUoW.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection()
    .AddPlan2Services()
    .BuildServiceProvider();

using var scope = services.CreateScope();
var svc = scope.ServiceProvider.GetRequiredService<IEntityCrudService>();

for (int i = 0; i < 5; i++)
{
    var id = await svc.CreateAsync($"demo-{i}", RandomScore());
    Console.WriteLine($"Created entity with ID: {id}");
    await Task.Delay(10);
}

Console.WriteLine("Done");

static double RandomScore() => Random.Shared.NextDouble() < 0.2
        ? Random.Shared.Next(0, 40)
        : Random.Shared.Next(60, 100);
