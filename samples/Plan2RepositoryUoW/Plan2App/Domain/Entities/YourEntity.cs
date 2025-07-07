using ExampleLib.Domain;
using ExampleData;

namespace Plan2RepositoryUoW.Domain.Entities;

public sealed class YourEntity : IValidatable, IBaseEntity, IRootEntity
{
    public int Id { get; set; } = Random.Shared.Next(1000, 9999);
    public string Name { get; set; } = string.Empty;
    public double Score { get; set; }
    public bool Validated { get; set; }
}
