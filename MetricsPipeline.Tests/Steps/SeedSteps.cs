using System.Text;
using MetricsPipeline.Core;
using MetricsPipeline.Infrastructure;
using MetricsPipeline.Seeding;
using Reqnroll;
using FluentAssertions;

namespace MetricsPipeline.Tests.Steps;

[Binding]
[Scope(Feature = "ValidateSeeding")]
public class SeedSteps
{
    private readonly IGenericRepository<SummaryRecord> _repo;
    private readonly IUnitOfWork _uow;
    private readonly ScenarioContext _ctx;
    private string _dir = string.Empty;

    public SeedSteps(IGenericRepository<SummaryRecord> repo, IUnitOfWork uow, ScenarioContext ctx)
    {
        _repo = repo;
        _uow = uow;
        _ctx = ctx;
    }

    [BeforeScenario]
    public void Setup()
    {
        _dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(_dir, "Seeds"));
    }

    [AfterScenario]
    public void Cleanup()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }

    [Given(@"a seed file ""(.*)"" containing:")]
    public void GivenSeedFile(string file, string content)
    {
        File.WriteAllText(Path.Combine(_dir, "Seeds", file), content.Trim());
    }

    [Given(@"the seeding service is executed")]
    [When(@"the seeding service is executed")]
    public async Task WhenServiceExecuted()
    {
        var svc = new SeedDataService(_repo, _uow, Path.Combine(_dir, "Seeds"));
        try
        {
            await svc.SeedAsync();
        }
        catch (Exception ex)
        {
            _ctx["error"] = ex;
        }
    }

    [When(@"executing the seeding service")]
    public async Task WhenExecutingService()
    {
        await WhenServiceExecuted();
    }

    [Then(@"the repository should contain (\d+) record(?:s)?")]
    public async Task ThenRepositoryCount(int expected)
    {
        var count = await _repo.GetCountAsync();
        count.Should().Be(expected);
    }

    [Then(@"a SeedValidationException should be thrown for ""(.*)"" line (\d+)")]
    public void ThenException(string file, int line)
    {
        _ctx.Should().ContainKey("error");
        var ex = _ctx["error"].Should().BeOfType<SeedValidationException>().Subject;
        ex.FileName.Should().Be(file);
        ex.LineNumber.Should().Be(line);
    }
}
