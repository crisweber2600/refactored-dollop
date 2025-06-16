using System.Linq.Expressions;

namespace MetricsPipeline.Core;

public interface IBaseEntity
{
    int Id { get; set; }
}

public interface IRootEntity { }

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}

public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }
}
