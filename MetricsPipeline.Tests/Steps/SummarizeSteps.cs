using MetricsPipeline.Core;
using Reqnroll;
using FluentAssertions;

[Binding]
public class SummarizeSteps
{
    private readonly ISummarizationService _sum;
    private readonly ScenarioContext _ctx;

    public SummarizeSteps(ISummarizationService sum, ScenarioContext ctx)
    {
        _sum = sum;
        _ctx = ctx;
    }

    [Given(@"the input metric values are:")]
    public void GivenInputMetricValues(Table table)
    {
        _ctx["metrics"] = table.Rows.Select(r => double.Parse(r[0])).ToList();
    }

    [Given(@"there are no metric values to summarize")]
    public void GivenNoMetrics()
    {
        _ctx["metrics"] = new List<double>();
    }

    [When(@"the system summarizes the values using ""(.*)""")]
    public void WhenSystemSummarizes(string strategy)
    {
        var metrics = (List<double>)_ctx["metrics"];
        var res = _sum.Summarize(metrics, Enum.Parse<SummaryStrategy>(strategy, true));
        _ctx["sumResult"] = res;
    }

    [When(@"the system attempts to summarize using ""(.*)""")]
    public void WhenAttemptSummarize(string strategy)
    {
        WhenSystemSummarizes(strategy);
    }

    [Then(@"the result should be ([0-9.]+)")]
    [Scope(Feature = "SummarizeMetricValues")]
    public void ThenResultShouldBeDouble(double expected)
    {
        var res = (PipelineResult<double>)_ctx["sumResult"];
        res.Value.Should().Be(expected);
    }

    [Then(@"the summarization should fail with reason ""(.*)""")]
    [Scope(Feature = "SummarizeMetricValues")]
    public void ThenSummarizationFailWith(string reason)
    {
        var res = (PipelineResult<double>)_ctx["sumResult"];
        res.IsSuccess.Should().BeFalse();
        res.Error.Should().Be(reason);
    }
}
