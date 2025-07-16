using ExampleLib.Domain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WorkerService1.Models
{
    public class SampleEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; } 
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; } // Numeric value for validation
        public bool Validated { get; set; } // Required by IValidatable
    }

    public class OtherEntity : IValidatable, IBaseEntity, IRootEntity
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int Amount { get; set; }
        public bool IsActive { get; set; }
        public bool Validated { get; set; }
    }
}