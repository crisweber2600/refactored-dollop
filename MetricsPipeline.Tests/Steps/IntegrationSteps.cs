using Reqnroll;
using MetricsPipeline.Core;
using MetricsPipeline.Infrastructure;
using FluentAssertions;

[Binding]
public class IntegrationSteps
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly ListGatherService _gather;
    private readonly SummaryDbContext _db;
    private PipelineResult<PipelineState<double>>? _run;

    public IntegrationSteps(IPipelineOrchestrator orchestrator, IGatherService gather, SummaryDbContext db)
    {
        _orchestrator = orchestrator;
        _gather = (ListGatherService)gather;
        _db = db;
    }

    private Uri _source = new("https://api.example.com/data");
    private double _threshold = 5.0;
    private string _pipeline = "default";
    private string _gatherMethod = "FetchMetricsAsync";

    [Given(@"the system is configured with a delta threshold of (.*)")]
    public void GivenThreshold(double t)
    {
        _threshold = t;
    }

    [Given("the pipeline name is \"(.*)\"")]
    public void GivenPipelineName(string name)
    {
        _pipeline = name;
    }

    [Given("the gather method is \"(.*)\"")]
    public void GivenGatherMethod(string method)
    {
        _gatherMethod = method;
    }

    [Given(@"the previously committed summary value is (.*)")]
    [Scope(Feature = "FullPipelineExecution")]
    public void GivenLastCommitted(double val)
    {
        _db.Summaries.Add(new SummaryRecord { PipelineName = _pipeline, Source = _source, Value = val, Timestamp = DateTime.UtcNow.AddMinutes(-10) });
        _db.SaveChanges();
    }

    [Given("pipeline \"(.*)\" has previously committed summary value (.*)")]
    public void GivenNamedPipelineLastCommitted(string name, double val)
    {
        _pipeline = name;
        GivenLastCommitted(val);
    }

    [Given("the gather service returns:")]
    public void GivenApiReturns(string endpoint, Table table)
    {
        var data = table.Rows.Select(r => double.Parse(r[0])).ToArray();
        _gather.Metrics = data;
        _source = new Uri(endpoint);
    }

    [Given("the gather service returns no metric values")]
    public void GivenApiOffline(string endpoint)
    {
        _gather.Metrics = Array.Empty<double>();
        _source = new Uri(endpoint);
    }

    [Given("the gather service returns no metric values")]
    public void GivenApiEmpty(string endpoint)
    {
        _gather.Metrics = Array.Empty<double>();
        _source = new Uri(endpoint);
    }

    [When(@"the pipeline is executed")]
    public async Task WhenPipelineExecuted()
    {
        _run = await _orchestrator.ExecuteAsync<double>(_pipeline, v => v, SummaryStrategy.Average, _threshold, default, _gatherMethod);
    }

    [When(@"pipeline ""(.*)"" is executed")]
    public async Task WhenNamedPipelineExecuted(string name)
    {
        _pipeline = name;
        await WhenPipelineExecuted();
    }

    [Then(@"the summary should be (.*)")]
    public void ThenSummaryShouldBe(double expected)
    {
        _run!.Value.Summary.Should().Be(expected);
    }

    [Then(@"the delta from last run should be (.*)")]
    public void ThenDelta(double delta)
    {
        var diff = Math.Abs(_run!.Value.Summary!.Value - _run!.Value.LastCommittedSummary!.Value);
        diff.Should().Be(delta);
    }

    [Then(@"the summary should be committed successfully")]
    public void ThenCommitted()
    {
        _run!.IsSuccess.Should().BeTrue();
    }

    [Then(@"the summary should not be committed")]
    public void ThenNotCommitted()
    {
        _run!.IsSuccess.Should().BeFalse();
    }

    [Then(@"a ""(.*)"" warning should be logged")]
    public void ThenWarningLogged(string reason)
    {
        _run!.Error.Should().Be(reason);
    }

    [Then(@"the operation should fail at the gather stage")]
    public void ThenFailGather()
    {
        _run!.Error.Should().Be("NoData");
    }

    [Then(@"the system should log an error with reason ""(.*)""")]
    public void ThenLogError(string reason)
    {
        _run!.Error.Should().Be(reason);
    }

    [Then(@"no summary should be computed or committed")]
    public void ThenNoSummary()
    {
        _run!.IsSuccess.Should().BeFalse();
    }

    [Then(@"the operation should halt at the summarize stage")]
    public void ThenHaltSummarize()
    {
        _run!.Error.Should().Be("NoData");
    }

    [Then(@"no validation or commit should occur")]
    public void ThenNoValidationOrCommit()
    {
        _run!.IsSuccess.Should().BeFalse();
    }
}
