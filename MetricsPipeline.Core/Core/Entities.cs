using System.Linq.Expressions;

namespace MetricsPipeline.Core;

public interface IBaseEntity
{
    int Id { get; set; }
}

public interface IEntity { }

public interface IRootEntity : IEntity { }

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}

public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }
}
