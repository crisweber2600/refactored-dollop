namespace ExampleData;

public interface IValidatable { bool Validated { get; set; } }
public interface IBaseEntity { int Id { get; set; } }
public abstract class BaseEntity : IValidatable, IBaseEntity, IRootEntity
{
    public int Id { get; set; }
    public bool Validated { get; set; }
}
public interface IRootEntity {}

public class YourEntity : IValidatable, IBaseEntity, IRootEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Validated { get; set; }
    public DateTime Timestamp { get; set; }
}

public class Nanny
{
    public int Id { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public double SummarizedValue { get; set; }
    public DateTime DateTime { get; set; }
    public Guid RuntimeID { get; set; }
}
