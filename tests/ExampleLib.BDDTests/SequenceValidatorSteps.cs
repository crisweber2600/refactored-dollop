using System;
using System.Collections.Generic;
using ExampleLib.Domain;
using Reqnroll;

namespace ExampleLib.BDDTests;

[Binding]
public class SequenceValidatorSteps
{
    private List<Foo> _items = new();
    private bool _result;

    [Given("a foo sequence that should pass")]
    public void GivenSequencePass()
    {
        _items = new List<Foo>
        {
            new Foo { Jar = 1, Car = 10 },
            new Foo { Jar = 1, Car = 11 },
            new Foo { Jar = 2, Car = 12 }
        };
    }

    [Given("a foo sequence that should fail")]
    public void GivenSequenceFail()
    {
        _items = new List<Foo>
        {
            new Foo { Jar = 1, Car = 10 },
            new Foo { Jar = 2, Car = 20 },
            new Foo { Jar = 1, Car = 30 }
        };
    }

    [When(@"validating with a delta rule of (\d+)")]
    public void WhenValidating(int delta)
    {
        _result = SequenceValidator.Validate(
            _items,
            x => x.Jar,
            x => x.Car,
            (cur, prev) => Math.Abs(cur - prev) <= delta);
    }

    [Then(@"the sequence validation result should be (true|false)")]
    public void ThenResult(bool expected)
    {
        if (_result != expected)
            throw new Exception($"Expected {expected} but was {_result}");
    }
}
