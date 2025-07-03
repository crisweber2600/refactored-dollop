using System.Threading;
using System.Threading.Tasks;
using ExampleData;

namespace ExampleLib.BDDTests;

public static class UnitOfWorkExtensions
{
    public static Task<int> SaveChangesAsync<TEntity>(this IUnitOfWork uow, ValidationRuleSet<TEntity> ruleSet, CancellationToken cancellationToken = default)
        where TEntity : class, IValidatable, IBaseEntity, IRootEntity
    {
        return ((dynamic)uow).SaveChangesAsync(ruleSet, cancellationToken);
    }
}
