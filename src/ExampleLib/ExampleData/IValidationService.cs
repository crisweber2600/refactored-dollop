namespace ExampleData;

using System.Linq.Expressions;

public interface IValidationService
{
    Task<double> ComputeAsync<TEntity>(Expression<Func<TEntity, double>> selector,
        ValidationStrategy strategy,
        CancellationToken cancellationToken = default) where TEntity : class;
}
