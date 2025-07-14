using System;
using System.Collections.Generic;
using ExampleLib.Domain;
using Xunit;

namespace ExampleLib.Tests;

public class SequenceValidatorTests
{
    [Fact]
    public void SequenceWithinDelta_ReturnsTrue()
    {
        var items = new List<Foo>
        {
            new Foo { Jar = 1, Car = 10 },
            new Foo { Jar = 1, Car = 11 },
            new Foo { Jar = 2, Car = 12 }
        };

        bool result = SequenceValidator.Validate(
            items,
            x => x.Jar,
            x => x.Car,
            (cur, prev) => Math.Abs(cur - prev) <= 2);

        Assert.True(result);
    }

    [Fact]
    public void SequenceExceedingDelta_ReturnsFalse()
    {
        var items = new List<Foo>
        {
            new Foo { Jar = 1, Car = 10 },
            new Foo { Jar = 2, Car = 20 },
            new Foo { Jar = 1, Car = 30 }
        };

        bool result = SequenceValidator.Validate(
            items,
            x => x.Jar,
            x => x.Car,
            (cur, prev) => Math.Abs(cur - prev) <= 5);

        Assert.False(result);
    }
}
