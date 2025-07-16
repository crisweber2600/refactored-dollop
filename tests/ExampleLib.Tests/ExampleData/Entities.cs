using ExampleLib.Domain;

namespace ExampleData;

public class YourEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
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
