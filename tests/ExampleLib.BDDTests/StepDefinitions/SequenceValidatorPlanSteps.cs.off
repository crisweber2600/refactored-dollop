using ExampleData;
using ExampleLib.Domain;
using Reqnroll;

namespace ExampleLib.BDDTests;

[Binding]
public class SequenceValidatorPlanSteps
{
    private SummarisationPlan<YourEntity>? _plan;
    private List<YourEntity> _items = new();
    private bool _result;

    [Given("a summarisation plan using (.*) threshold (.*)")]
    public void GivenPlan(string type, decimal threshold)
    {
        var t = Enum.Parse<ThresholdType>(type);
        _plan = new SummarisationPlan<YourEntity>(e => e.Id, t, threshold);
    }

    [Given("a sequence \"(.*)\" for server \"(.*)\"")]
    public void GivenSequence(string csv, string server)
    {
        _items = csv.Split(',')
            .Select(v => new YourEntity { Id = int.Parse(v), Name = server })
            .ToList();
    }

    [When("validating the sequence by server")]
    public void WhenValidating()
    {
        _result = SequenceValidator.Validate(_items, e => e.Name, _plan!);
    }

    [Then("the validation result should be (true|false)")]
    public void ThenResult(bool expected)
    {
        if (_result != expected)
            throw new Exception($"Expected {expected} but was {_result}");
    }
}
