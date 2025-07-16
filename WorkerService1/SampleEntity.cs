using ExampleLib.Domain;

namespace WorkerService1.Models
{
    public class SampleEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; } // Numeric value for validation
        public bool Validated { get; set; } // Required by IValidatable
    }
}