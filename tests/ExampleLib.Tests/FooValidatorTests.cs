using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Xunit;

namespace ExampleLib.Tests;

public class FooValidatorTests
{
    [Fact]
    public void NoPreviousAudit_ReturnsTrue()
    {
        var repo = new InMemorySaveAuditRepository();
        var validator = new FooValidator(repo);
        var foo = new Foo { Id = 1, Description = "svc2", Jar = 5 };
        Assert.True(validator.Validate(foo));
    }

    [Fact]
    public void MatchingJar_ReturnsTrue()
    {
        var repo = new InMemorySaveAuditRepository();
        repo.AddAudit(new SaveAudit { EntityType = nameof(Foo), EntityId = "1", Jar = 5 });
        var validator = new FooValidator(repo);
        var foo = new Foo { Id = 1, Description = "svc2", Jar = 5 };
        Assert.True(validator.Validate(foo));
    }

    [Fact]
    public void DifferentJar_ReturnsFalse()
    {
        var repo = new InMemorySaveAuditRepository();
        repo.AddAudit(new SaveAudit { EntityType = nameof(Foo), EntityId = "1", Jar = 5 });
        var validator = new FooValidator(repo);
        var foo = new Foo { Id = 1, Description = "svc2", Jar = 2 };
        Assert.False(validator.Validate(foo));
    }
}
