using MetricsPipeline.Core;
using Reqnroll;
using FluentAssertions;

[Binding]
public class ValidateSteps
{
    private readonly IValidationService _val;
    private readonly ScenarioContext _ctx;

    public ValidateSteps(IValidationService val, ScenarioContext ctx)
    {
        _val = val;
        _ctx = ctx;
    }

    [Given(@"the last committed summary value is (.*)")]
    public void GivenLastCommitted(double last)
    {
        _ctx["last"] = last;
    }

    [Given(@"the configured maximum delta is (.*)")]
    public void GivenMaxDelta(double delta)
    {
        _ctx["delta"] = delta;
    }

    [Given(@"the current summary value is (.*)")]
    public void GivenCurrent(double current)
    {
        _ctx["current"] = current;
    }

    [When(@"the delta is calculated")]
    public void WhenDeltaCalculated()
    {
        var res = _val.IsWithinThreshold((double)_ctx["current"], (double)_ctx["last"], (double)_ctx["delta"]);
        _ctx["valResult"] = res;
        _ctx["deltaValue"] = Math.Abs((double)_ctx["current"] - (double)_ctx["last"]);
    }

    [Then(@"the delta should be (.*)")]
    public void ThenDeltaShouldBe(double expected)
    {
        ((double)_ctx["deltaValue"]).Should().Be(expected);
    }

    [Then(@"the summary should be marked as (.*)")]
    public void ThenMarkedAs(string expected)
    {
        var res = (PipelineResult<bool>)_ctx["valResult"];
        var isValid = res.Value;
        (expected == "valid" ? true : false).Should().Be(isValid);
    }
}
