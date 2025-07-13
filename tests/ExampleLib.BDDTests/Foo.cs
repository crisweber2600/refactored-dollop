using ExampleData;

namespace ExampleLib.BDDTests;

public class Foo : IValidatable, IBaseEntity, IRootEntity
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool Validated { get; set; }
}
