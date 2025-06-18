namespace ExampleData;

public interface IValidatable { bool Validated { get; set; } }
public interface IBaseEntity { int Id { get; set; } }
public interface IRootEntity {}

public class YourEntity : IValidatable, IBaseEntity, IRootEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Validated { get; set; }
    public DateTime Timestamp { get; set; }
}
