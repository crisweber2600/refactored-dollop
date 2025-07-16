using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExampleLib.Domain;
using MongoDB.Bson.Serialization.Attributes;

namespace WorkerService1.Models
{
    public class SampleEntity : IValidatable, IBaseEntity, IRootEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [BsonId]
        /// <summary>
        /// Do not set this property manually. It is set by the database or MongoDB.
        /// </summary>
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; } // Numeric value for validation
        public bool Validated { get; set; } // Required by IValidatable
    }

    public class OtherEntity : IValidatable, IBaseEntity, IRootEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [BsonId]
        /// <summary>
        /// Do not set this property manually. It is set by the database or MongoDB.
        /// </summary>
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int Amount { get; set; }
        public bool IsActive { get; set; }
        public bool Validated { get; set; }
    }
}