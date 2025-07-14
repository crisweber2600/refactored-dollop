using ExampleLib.Domain;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Manual validator for <see cref="tests.ExampleLib.BDDTests.Foo"/> instances.
/// Compares the current Jar value with the last stored audit when the
/// description is "svc2".
/// </summary>
public class FooValidator
{
    private readonly ISaveAuditRepository _repository;
    public FooValidator(ISaveAuditRepository repository)
    {
        _repository = repository;
    }

    public bool Validate(object instance)
    {
        var foo = instance as dynamic; // dynamic to avoid direct reference
        if (foo == null)
            return true;
        string id = foo.Id.ToString();
        string desc = foo.Description as string;
        decimal jar = foo.Jar;
        var previous = _repository.GetLastAudit("Foo", id);
        if (desc == "svc2" && previous != null)
        {
            return previous.Jar == jar;
        }
        return true;
    }
}
