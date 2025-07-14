using ExampleLib.Infrastructure;
using Xunit;

namespace ExampleLib.Tests;

public class SequenceValidatorTests
{
    private class Foo
    {
        public string Jar { get; set; } = string.Empty;
        public decimal Car { get; set; }
    }

    [Fact]
    public void Validates_WhenKeyChanges()
    {
        var validator = new SequenceValidator<Foo, string>(
            f => f.Jar,
            f => f.Car,
            (cur, prev) => cur - prev <= 1);

        Assert.True(validator.Validate(new Foo { Jar = "svc1", Car = 1 }));
        Assert.True(validator.Validate(new Foo { Jar = "svc1", Car = 2 }));
        Assert.True(validator.Validate(new Foo { Jar = "svc2", Car = 3 }));
        Assert.False(validator.Validate(new Foo { Jar = "svc3", Car = 10 }));
    }
}
