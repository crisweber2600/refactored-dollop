using ExampleLib.Domain;

namespace Plan2RepositoryUoW.Domain.Entities;

public sealed class YourEntity : IValidatable, IBaseEntity, IRootEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public double Score { get; set; }
    public bool Validated { get; set; }
}
