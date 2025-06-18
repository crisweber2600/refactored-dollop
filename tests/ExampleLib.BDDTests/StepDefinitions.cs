using ExampleLib;
using Reqnroll;

namespace ExampleLib.BDDTests;

[Binding]
public class StepDefinitions
{
    private readonly ICalculator _calculator;
    private int _result;

    public StepDefinitions(ICalculator calculator)
    {
        _calculator = calculator;
    }

    [Given("I have two numbers (\\d+) and (\\d+)")]
    public void GivenIHaveTwoNumbers(int a, int b)
    {
        // numbers are stored directly in step parameters
        _result = _calculator.Add(a, b);
    }

    [When("they are added")]
    public void WhenTheyAreAdded()
    {
        // operation already done in Given step
    }

    [Then("the result should be (\\d+)")]
    public void ThenTheResultShouldBe(int expected)
    {
        if (_result != expected)
        {
            throw new Exception($"Expected {expected} but was {_result}");
        }
    }
}
