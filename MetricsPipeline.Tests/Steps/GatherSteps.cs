using MetricsPipeline.Core;
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

    [Given(@"the API endpoint \"(.*)\" is available")]
    public void GivenEndpointAvailable(string raw)
    {
        _ctx["endpoint"] = new Uri(raw);
    }

    [Given(@"the API endpoint \"(.*)\" is down")]
    public void GivenEndpointDown(string raw)
    {
        _ctx["endpoint"] = new Uri(raw);
    }

    [When(@"the system requests metric data")]
    public async Task WhenSystemRequestsMetricData()
    {
        var result = await _gather.FetchMetricsAsync((Uri)_ctx["endpoint"]);
        _ctx["gatherResult"] = result;
    }

    [Then(@"the API should respond with HTTP (.*)")]
    public void ThenApiShouldRespondWith(int status)
    {
        var res = (PipelineResult<IReadOnlyList<double>>)_ctx["gatherResult"];
        (status == 200 ? res.IsSuccess : !res.IsSuccess).Should().BeTrue();
    }

    [Then(@"the response should contain metric values")]
    public void ThenResponseContainsValues(Table table)
    {
        var res = (PipelineResult<IReadOnlyList<double>>)_ctx["gatherResult"];
        res.IsSuccess.Should().BeTrue();
        res.Value.Should().BeEquivalentTo(table.Rows.Select(r => double.Parse(r[0])));
    }
}
