using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using MetricsPipeline.Core.Domain;
using MetricsPipeline.Core.Infrastructure.Workers;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

public class Program
{
    public static async Task Main()
    {
        var services = new ServiceCollection();
        services.AddSaveValidation<ExampleEntity>(e => e.Values.Sum(), ThresholdType.PercentChange, 0.5m);
        services.AddSingleton<ValueProvider>();
        services.AddTransient<ExampleWorker>();

        var provider = services.BuildServiceProvider();
        var worker = provider.GetRequiredService<ExampleWorker>();
        var repo = provider.GetRequiredService<ISaveAuditRepository>();

        Console.WriteLine("Initial save:");
        await worker.SaveInitialAsync();
        ShowAudit(repo);

        Console.WriteLine("Saving inside threshold:");
        await worker.SaveWithinAsync();
        ShowAudit(repo);

        Console.WriteLine("Saving outside threshold:");
        await worker.SaveOutsideAsync();
        ShowAudit(repo);
    }

    private static void ShowAudit(ISaveAuditRepository repo)
    {
        var audit = repo.GetLastAudit(nameof(ExampleEntity), "EXAMPLE");
        if (audit != null)
            Console.WriteLine($"Metric={audit.MetricValue}, Validated={audit.Validated}");
    }
}
