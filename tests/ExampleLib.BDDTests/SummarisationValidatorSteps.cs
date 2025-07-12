using ExampleData;
using ExampleLib.Domain;
using SaveAudit = ExampleLib.Domain.SaveAudit;
using Reqnroll;

namespace ExampleLib.BDDTests;

[Binding]
public class SummarisationValidatorSteps
{
    private readonly ISummarisationValidator<YourEntity> _validator;
    private ValidationPlan<YourEntity>? _plan;
    private SaveAudit? _previous;
    private YourEntity _entity = new();
    private bool _result;

    public SummarisationValidatorSteps(ISummarisationValidator<YourEntity> validator)
    {
        _validator = validator;
    }

    [Given("a summarisation plan using (.*) threshold (.*)")]
    public void GivenAPlan(string type, decimal threshold)
    {
        var t = Enum.Parse<ThresholdType>(type);
        _plan = new ValidationPlan<YourEntity>(e => e.Id, t, threshold);
    }

    [Given("no previous audit")]
    public void GivenNoPreviousAudit()
    {
        _previous = null;
    }

    [Given("a previous audit with metric (.*)")]
    public void GivenPreviousAudit(decimal value)
    {
        _previous = new SaveAudit { MetricValue = value };
    }

    [Given("the current metric is (.*)")]
    public void GivenCurrentMetric(decimal value)
    {
        _entity.Id = (int)value;
    }

    [When("validating the save")]
    public void WhenValidatingTheSave()
    {
        _result = _validator.Validate(_entity, _previous, _plan!);
    }

    [Then("the validation result should be (true|false)")]
    public void ThenTheValidationResultShouldBe(bool expected)
    {
        if (_result != expected)
            throw new Exception($"Expected {expected} but was {_result}");
    }
}
