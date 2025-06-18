using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using MetricsPipeline.Core.Domain;
using MetricsPipeline.Core.Infrastructure.Workers;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using System.Threading.Tasks;

namespace ExampleLib.BDDTests;

[Binding]
public class ExampleWorkerSteps
{
    private ServiceProvider? _provider;
    private ExampleWorker? _worker;
    private InMemorySaveAuditRepository? _auditRepo;


    [Given("an example worker")]
    public async Task GivenAnExampleWorker()
    {
        var services = new ServiceCollection();
        services.AddSaveValidation<ExampleEntity>(e => e.Values.Sum(), ThresholdType.PercentChange, 0.5m);
        services.AddSingleton<ValueProvider>();
        services.AddTransient<ExampleWorker>();

        _provider = services.BuildServiceProvider();
        _worker = _provider.GetRequiredService<ExampleWorker>();
        _auditRepo = (InMemorySaveAuditRepository)_provider.GetRequiredService<ISaveAuditRepository>();
    }

    [When("the initial values are saved")]
    public async Task WhenInitialValuesSaved()
    {
        if (_worker != null)
        {
            await _worker.SaveInitialAsync();
        }
    }

    [Then("the audit should be valid")]
    public void ThenAuditValid()
    {
        var audit = _auditRepo!.GetLastAudit(nameof(ExampleEntity), "EXAMPLE");
        if (audit == null || !audit.Validated)
            throw new Exception("Expected audit to be valid");
    }

    [When("values inside the threshold are saved")]
    public async Task WhenValuesInsideSaved()
    {
        if (_worker != null)
        {
            await _worker.SaveWithinAsync();
        }
    }

    [Then("the audit should still be valid")]
    public void ThenAuditStillValid()
    {
        var audit = _auditRepo!.GetLastAudit(nameof(ExampleEntity), "EXAMPLE");
        if (audit == null || !audit.Validated)
            throw new Exception("Expected audit to remain valid");
    }

    [When("values outside the threshold are saved")]
    public async Task WhenValuesOutsideSaved()
    {
        if (_worker != null)
        {
            await _worker.SaveOutsideAsync();
        }
    }

    [Then("the audit should be invalid")]
    public void ThenAuditInvalid()
    {
        var audit = _auditRepo!.GetLastAudit(nameof(ExampleEntity), "EXAMPLE");
        if (audit == null || audit.Validated)
            throw new Exception("Expected audit to be invalid");
    }

    [AfterScenario]
    public async Task Cleanup()
    {
        if (_provider != null)
            await _provider.DisposeAsync();
    }
}
