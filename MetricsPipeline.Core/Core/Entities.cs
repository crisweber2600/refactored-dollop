using System.Linq.Expressions;

namespace MetricsPipeline.Core;

/// <summary>
/// Base entity with an integer identifier.
/// </summary>
public interface IBaseEntity
{
    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    int Id { get; set; }
}

/// <summary>
/// Marker interface for entities.
/// </summary>
public interface IEntity { }

/// <summary>
/// Marker interface for root aggregate entities.
/// </summary>
public interface IRootEntity : IEntity { }

/// <summary>
/// Indicates an entity supports soft deletion.
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// Gets or sets whether the entity has been soft deleted.
    /// </summary>
    bool IsDeleted { get; set; }
}

/// <summary>
/// Specification used for repository queries.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Expression defining the criteria for the specification.
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }
}
