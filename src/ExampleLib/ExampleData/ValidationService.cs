using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace ExampleData;

public class ValidationService : IValidationService
{
    private readonly YourDbContext _context;

    public ValidationService(YourDbContext context)
    {
        _context = context;
    }

    public async Task<double> ComputeAsync<TEntity>(Expression<Func<TEntity, double>> selector,
        ValidationStrategy strategy,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var set = _context.Set<TEntity>().IgnoreQueryFilters();
        switch (strategy)
        {
            case ValidationStrategy.Sum:
                return await set.SumAsync(selector, cancellationToken);
            case ValidationStrategy.Average:
                return await set.AverageAsync(selector, cancellationToken);
            case ValidationStrategy.Count:
                return await set.CountAsync(cancellationToken);
            case ValidationStrategy.Variance:
                var values = await set.Select(selector).ToListAsync(cancellationToken);
                if (values.Count == 0) return 0;
                var avg = values.Average();
                return values.Sum(v => Math.Pow(v - avg, 2)) / values.Count;
            default:
                throw new ArgumentOutOfRangeException(nameof(strategy));
        }
    }
}
