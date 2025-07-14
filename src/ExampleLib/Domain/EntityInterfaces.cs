namespace ExampleLib.Domain;

public interface IValidatable { bool Validated { get; set; } }
public interface IBaseEntity { int Id { get; set; } }
public interface IRootEntity { }
public abstract class BaseEntity : IValidatable, IBaseEntity, IRootEntity
{
    public int Id { get; set; }
    public bool Validated { get; set; }
}
