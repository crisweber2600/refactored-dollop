using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace ExampleData;

/// <summary>
/// Decorates <see cref="IMongoDbService"/> to run ExampleLib validation
/// after inserting documents.
/// </summary>
public class MongoDbServiceValidationDecorator : IMongoDbService
{
    private readonly IMongoDbService _inner;
    private readonly IUnitOfWork _uow;

    public MongoDbServiceValidationDecorator(IMongoDbService inner, IUnitOfWork uow)
    {
        _inner = inner;
        _uow = uow;
    }

    public async Task InsertManyItemsAsync<T>(IEnumerable<T> items, string collectionName)
    {
        await _inner.InsertManyItemsAsync(items, collectionName);
        var t = typeof(T);
        if (typeof(IValidatable).IsAssignableFrom(t) &&
            typeof(IBaseEntity).IsAssignableFrom(t) &&
            typeof(IRootEntity).IsAssignableFrom(t))
        {
            var method = typeof(IUnitOfWork)
                .GetMethod(nameof(IUnitOfWork.SaveChangesWithPlanAsync))!
                .MakeGenericMethod(t);
            await (Task<int>)method.Invoke(_uow, new object?[] { CancellationToken.None })!;
        }
    }
}
