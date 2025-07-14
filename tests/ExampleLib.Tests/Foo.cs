using ExampleData;

namespace ExampleLib.Tests;

public class Foo : IValidatable, IBaseEntity, IRootEntity
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Jar { get; set; }
    public bool Validated { get; set; }
}
