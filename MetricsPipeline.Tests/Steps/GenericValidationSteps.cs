using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MetricsPipeline.Core;
using Reqnroll;

namespace MetricsPipeline.Tests.Steps;

internal class ValueItem { public double Amount { get; set; } }

[Binding]
[Scope(Feature = "GenericValidation")]
public class GenericValidationSteps
{
    private readonly IValidationService _val;
    private readonly List<ValueItem> _items = new();
    private double _last;
    private double _delta;
    private PipelineResult<bool>? _result;

    public GenericValidationSteps(IValidationService val)
    {
        _val = val;
    }

    [Given("the last committed summary value is (.*)")]
    public void GivenLast(double last) => _last = last;

    [Given("the configured maximum delta is (.*)")]
    public void GivenDelta(double delta) => _delta = delta;

    [Given("the following items exist:")]
    public void GivenItems(Table table)
    {
        foreach (var row in table.Rows)
            _items.Add(new ValueItem { Amount = double.Parse(row[0]) });
    }

    [When("the list is validated by summing Amount")]
    public void WhenValidatedSum()
    {
        _result = _val.IsWithinThreshold(_items, i => i.Amount, SummaryStrategy.Sum, _last, _delta);
    }

    [When("the list is validated by averaging Amount")]
    public void WhenValidatedAverage()
    {
        _result = _val.IsWithinThreshold(_items, i => i.Amount, SummaryStrategy.Average, _last, _delta);
    }

    [Then("the summary should be marked as (.*)")]
    public void ThenResult(string outcome)
    {
        _result!.IsSuccess.Should().BeTrue();
        var expected = outcome == "valid";
        _result.Value.Should().Be(expected);
    }
}
