using System.Linq.Expressions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ExampleData;

public class MongoValidationService : IValidationService
{
    private readonly IMongoDatabase _database;

    public MongoValidationService(IMongoDatabase database)
    {
        _database = database;
    }

    public async Task<double> ComputeAsync<TEntity>(Expression<Func<TEntity, double>> selector,
        ValidationStrategy strategy,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var collection = _database.GetCollection<TEntity>(typeof(TEntity).Name);
        var items = await collection.AsQueryable().ToListAsync(cancellationToken);
        var values = items.Select(selector.Compile()).ToList();
        switch (strategy)
        {
            case ValidationStrategy.Sum:
                return values.Sum();
            case ValidationStrategy.Average:
                return values.Count == 0 ? 0 : values.Average();
            case ValidationStrategy.Count:
                return values.Count;
            case ValidationStrategy.Variance:
                if (values.Count == 0) return 0;
                var avg = values.Average();
                return values.Sum(v => Math.Pow(v - avg, 2)) / values.Count;
            default:
                throw new ArgumentOutOfRangeException(nameof(strategy));
        }
    }
}
