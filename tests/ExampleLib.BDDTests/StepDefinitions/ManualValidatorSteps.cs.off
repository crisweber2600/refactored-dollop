using ExampleLib.Domain;
using Reqnroll;
using System;
using System.Collections.Generic;

namespace ExampleLib.BDDTests;

[Binding]
public class ManualValidatorSteps
{
    private IManualValidatorService? _service;
    private readonly object _instance = new();
    private bool _result;

    [Given("a manual validator with no rules")]
    public void GivenValidatorWithNoRules()
    {
        _service = new ManualValidatorService(new Dictionary<Type, List<Func<object, bool>>>());
    }

    [Given("a manual validator with a rule that returns (true|false)")]
    public void GivenValidatorWithRule(bool value)
    {
        var rules = new Dictionary<Type, List<Func<object, bool>>>
        {
            { typeof(object), new List<Func<object, bool>> { _ => value } }
        };
        _service = new ManualValidatorService(rules);
    }

    [When("I validate the instance")]
    public void WhenIValidateTheInstance()
    {
        _result = _service!.Validate(_instance);
    }

    [Then("the manual validation result should be (true|false)")]
    public void ThenResultShouldBe(bool expected)
    {
        if (_result != expected)
            throw new Exception($"Expected {expected} but was {_result}");
    }
}
