using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using MassTransit;
using MetricsPipeline.Core.Domain;
using MetricsPipeline.Core.Infrastructure.Workers;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;

namespace ExampleLib.Tests;

public class ExampleWorkerTests
{
    [Fact]
    public async Task Worker_Validates_Values()
    {
        var services = new ServiceCollection();
        services.AddSaveValidation<ExampleEntity>(e => e.Values.Sum(), ThresholdType.PercentChange, 0.5m);
        services.AddSingleton<ValueProvider>();
        services.AddTransient<ExampleWorker>();

        var provider = services.BuildServiceProvider();
        var worker = provider.GetRequiredService<ExampleWorker>();
        var repo = provider.GetRequiredService<ISaveAuditRepository>();

        await worker.SaveInitialAsync();
        var audit = repo.GetLastAudit(nameof(ExampleEntity), "EXAMPLE");
        Assert.True(audit != null && audit.Validated);

        await worker.SaveWithinAsync();
        audit = repo.GetLastAudit(nameof(ExampleEntity), "EXAMPLE");
        Assert.True(audit != null && audit.Validated);

        await worker.SaveOutsideAsync();
        audit = repo.GetLastAudit(nameof(ExampleEntity), "EXAMPLE");
        Assert.True(audit != null && !audit.Validated);
        }
    }
