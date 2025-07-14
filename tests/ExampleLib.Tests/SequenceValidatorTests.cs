using ExampleLib.Domain;
using System;

namespace ExampleLib.Tests;

public class SequenceValidatorTests
{
    private class Foo
    {
        public string Jar { get; set; } = string.Empty;
        public int Car { get; set; }
    }

    [Fact]
    public void Validate_WithCustomFunc_Passes()
    {
        var data = new List<Foo>
        {
            new Foo { Jar = "server2", Car = 5 },
            new Foo { Jar = "server1", Car = 10 },
            new Foo { Jar = "server2", Car = 12 }
        };

        var result = SequenceValidator.Validate(
            data,
            x => x.Jar,
            x => x.Car,
            (current, previous) => Math.Abs(current - previous) <= 10);

        Assert.True(result);
    }

    [Fact]
    public void Validate_DefaultEquality_Passes()
    {
        var data = new List<Foo>
        {
            new Foo { Jar = "a", Car = 1 },
            new Foo { Jar = "b", Car = 1 },
            new Foo { Jar = "b", Car = 1 },
            new Foo { Jar = "a", Car = 1 }
        };

        Assert.True(SequenceValidator.Validate(data, f => f.Jar, f => f.Car));
    }

    [Fact]
    public void Validate_Fails_WhenRuleBroken()
    {
        var data = new List<Foo>
        {
            new Foo { Jar = "server2", Car = 5 },
            new Foo { Jar = "server1", Car = 50 },
            new Foo { Jar = "server2", Car = 12 }
        };

        var result = SequenceValidator.Validate(
            data,
            x => x.Jar,
            x => x.Car,
            (current, previous) => Math.Abs(current - previous) <= 10);

        Assert.False(result);
    }

    [Fact]
    public void Validate_WithPlan_Passes()
    {
        var data = new List<Foo>
        {
            new Foo { Jar = "a", Car = 10 },
            new Foo { Jar = "a", Car = 15 },
            new Foo { Jar = "a", Car = 18 }
        };

        var plan = new SummarisationPlan<Foo>(f => f.Car, ThresholdType.RawDifference, 5);

        Assert.True(SequenceValidator.Validate(data, f => f.Jar, plan));
    }

    [Fact]
    public void Validate_WithPlan_Fails()
    {
        var data = new List<Foo>
        {
            new Foo { Jar = "a", Car = 10 },
            new Foo { Jar = "a", Car = 20 },
            new Foo { Jar = "a", Car = 18 }
        };

        var plan = new SummarisationPlan<Foo>(f => f.Car, ThresholdType.RawDifference, 5);

        Assert.False(SequenceValidator.Validate(data, f => f.Jar, plan));
    }
}
