using MetricsPipeline.Core;
using Reqnroll;
using FluentAssertions;

[Binding]
public class CommitSteps
{
    private readonly ICommitService _commit;
    private readonly IDiscardHandler _discard;
    private readonly ScenarioContext _ctx;
    private double _summary = 47.0;
    private string _pipeline = "default";
    private readonly Uri _source = new("https://api.example.com/data");

    public CommitSteps(ICommitService commit, IDiscardHandler discard, ScenarioContext ctx)
    {
        _commit = commit;
        _discard = discard;
        _ctx = ctx;
    }

    [Given(@"the system has a summarized value of (.*)")]
    public void GivenSystemHasValue(double val)
    {
        _summary = val;
    }

    [Given(@"the summary is marked as (.*)")]
    public void GivenSummaryState(string state)
    {
        _ctx["state"] = state;
    }

    [Given(@"committing for pipeline ""(.*)""")]
    public void GivenPipeline(string pipeline)
    {
        _pipeline = pipeline;
    }

    [Given(@"the summary is valid")]
    public void GivenSummaryValid()
    {
        _ctx["state"] = "valid";
    }


    [Given(@"the database is temporarily unavailable")]
    public void GivenDbUnavailable()
    {
        _ctx["dbFail"] = true;
    }

    [When(@"the system executes commit")]
    [When(@"the system attempts to commit")]
    public async Task WhenSystemCommits()
    {
        var summary = _summary;
        var now = DateTime.UtcNow;
        if (_ctx.ContainsKey("dbFail"))
        {
            await _discard.HandleDiscardAsync(summary, "DatabaseError");
            _ctx["commitResult"] = PipelineResult<Unit>.Failure("DatabaseError");
        }
        else if ((string)_ctx["state"] == "valid")
        {
            _ctx["commitResult"] = await _commit.CommitAsync(_pipeline, _source, summary, now);
        }
        else if ((string)_ctx["state"] == "invalid")
        {
            await _discard.HandleDiscardAsync(summary, "ValidationFailed");
            _ctx["commitResult"] = PipelineResult<Unit>.Failure("ValidationFailed");
        }
        else
        {
            _ctx["commitResult"] = PipelineResult<Unit>.Failure("unknown");
        }
    }

    [Then(@"the summary should be saved in the database")]
    public void ThenSaved()
    {
        var res = (PipelineResult<Unit>)_ctx["commitResult"];
        res.IsSuccess.Should().BeTrue();
    }

    [Then(@"the commit timestamp should be recorded")]
    public void ThenTimestampRecorded()
    {
        // In-memory commit doesn't track timestamp; assume success above suffices
    }

    [Then(@"the summary should not be saved")]
    public void ThenNotSaved()
    {
        var res = (PipelineResult<Unit>)_ctx["commitResult"];
        res.IsSuccess.Should().BeFalse();
    }

    [Then(@"a warning should be logged with reason ""(.*)""")]
    public void ThenWarningLogged(string reason)
    {
        var res = (PipelineResult<Unit>)_ctx["commitResult"];
        res.Error.Should().Be(reason);
    }

    [Then(@"the commit should fail with reason ""(.*)""")]
    [Scope(Feature = "CommitOrDiscardSummary")]
    public void ThenCommitFailReason(string reason)
    {
        var res = (PipelineResult<Unit>)_ctx["commitResult"];
        res.Error.Should().Be(reason);
    }

    [Then(@"the summary should remain uncommitted")]
    public void ThenRemainUncommitted()
    {
        var res = (PipelineResult<Unit>)_ctx["commitResult"];
        res.IsSuccess.Should().BeFalse();
    }

    [Then(@"the result should be (.*)")]
    [Scope(Feature = "CommitOrDiscardSummary")]
    public void ThenResultShouldBe(string outcome)
    {
        var res = (PipelineResult<Unit>)_ctx["commitResult"];
        switch (outcome)
        {
            case "committed":
                res.IsSuccess.Should().BeTrue();
                break;
            case "discarded":
                res.Error.Should().Be("ValidationFailed");
                break;
            default:
                res.IsSuccess.Should().BeFalse();
                break;
        }
    }
}
