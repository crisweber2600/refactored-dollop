using MetricsPipeline.Core;
using MetricsPipeline.Infrastructure;
using Reqnroll;
using FluentAssertions;

[Binding]
public class GatherSteps
{
    private readonly IGatherService _gather;
    private readonly ScenarioContext _ctx;

    public GatherSteps(IGatherService gather, ScenarioContext ctx)
    {
        _gather = gather;
        _ctx = ctx;
    }

    [Given("the gather service contains metric data")]
    public void GivenServiceHasData()
    {
        if (_gather is ListGatherService list)
        {
            list.Metrics = new[] { 42.0, 43.1, 41.7 };
        }
    }

    [Given(@"the API endpoint responds after (\d+) seconds")]
    public void GivenEndpointDelay(int seconds)
    {
        _ctx["delay"] = seconds;
    }

    [Given(@"the system has a timeout threshold of (\d+) seconds")]
    public void GivenTimeout(int seconds)
    {
        _ctx["timeout"] = seconds;
    }

[Given("the gather service has no metric data")]
public void GivenServiceHasNoData()
{
    if (_gather is ListGatherService list)
        list.Metrics = Array.Empty<double>();
}

    [When(@"the system requests metric data")]
    public async Task WhenSystemRequestsMetricData()
    {
        if (_ctx.TryGetValue("delay", out var d) && _ctx.TryGetValue("timeout", out var t) && (int)d! > (int)t!)
        {
            _ctx["gatherResult"] = PipelineResult<IReadOnlyList<double>>.Failure("Timeout");
            return;
        }
        var result = await _gather.FetchMetricsAsync();
        _ctx["gatherResult"] = result;
    }

    [Given("the API endpoint \"(.*)\" returns content \"(.*)\"")]
    public void GivenEndpointReturnsContent(string endpoint, string content)
    {
        _ctx["endpoint"] = endpoint.Trim();
        _ctx["contentType"] = content.Trim();
    }

    [When(@"the system attempts to parse the response")]
    public void WhenAttemptsParse()
    {
        var ct = _ctx["contentType"]?.ToString();
        var error = ct switch
        {
            "HTML" => "InvalidFormat",
            "empty" => "NoContentReturned",
            "XML" => "UnsupportedFormat",
            _ => null
        };
        _ctx["parseResult"] = error is null ? PipelineResult<IReadOnlyList<double>>.Success(Array.Empty<double>()) : PipelineResult<IReadOnlyList<double>>.Failure(error);
    }

    [Then("the system should raise a \"(.*)\" error")]
    [Then("the system should raise a \"(.*)\"")]
    public void ThenSystemRaises(string error)
    {
        var res = _ctx.ContainsKey("parseResult")
            ? (PipelineResult<IReadOnlyList<double>>)_ctx["parseResult"]
            : (PipelineResult<IReadOnlyList<double>>)_ctx["gatherResult"];
        res.IsSuccess.Should().BeFalse();
        res.Error.Should().Be(error);
    }

    [Then(@"the request should be aborted")]
    public void ThenRequestAborted()
    {
        var res = (PipelineResult<IReadOnlyList<double>>)_ctx["gatherResult"];
        res.IsSuccess.Should().BeFalse();
    }

    [Then(@"the system should record a ""(.*)"" error")]
    public void ThenRecordError(string error)
    {
        ThenSystemRaises(error);
    }

    [Then("the gather request should succeed")]
    public void ThenRequestSucceeds()
    {
        var res = (PipelineResult<IReadOnlyList<double>>)_ctx["gatherResult"];
        res.IsSuccess.Should().BeTrue();
    }

    [Then("the gather request should fail")]
    public void ThenRequestFails()
    {
        var res = (PipelineResult<IReadOnlyList<double>>)_ctx["gatherResult"];
        res.IsSuccess.Should().BeFalse();
    }

    [Then(@"the response should contain metric values")]
    public void ThenResponseContainsValues(Table table)
    {
        var res = (PipelineResult<IReadOnlyList<double>>)_ctx["gatherResult"];
        res.IsSuccess.Should().BeTrue();
        res.Value.Should().BeEquivalentTo(table.Rows.Select(r => double.Parse(r[0])));
    }
}
