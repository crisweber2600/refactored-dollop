using System.Linq.Expressions;

namespace ExampleLib.Domain;

public interface IUnitOfWork
{
    IGenericRepository<T> Repository<T>() where T : class, IValidatable, IBaseEntity, IRootEntity;
    Task<int> SaveChangesAsync();
    Task<int> SaveChangesAsync<TEntity>(Expression<Func<TEntity, double>> selector,
        ValidationStrategy strategy,
        double threshold,
        CancellationToken cancellationToken = default)
        where TEntity : class, IValidatable, IBaseEntity, IRootEntity;
    Task<int> SaveChangesWithPlanAsync<TEntity>(CancellationToken cancellationToken = default)
        where TEntity : class, IValidatable, IBaseEntity, IRootEntity;
}
