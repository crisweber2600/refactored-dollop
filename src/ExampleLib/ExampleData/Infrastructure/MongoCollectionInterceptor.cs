using MongoDB.Driver;
using ExampleLib.Domain;

namespace ExampleData.Infrastructure;

/// <summary>
/// Wraps <see cref="IMongoCollection{T}"/> and invokes <see cref="IUnitOfWork.SaveChangesWithPlanAsync"/>
/// after inserts or updates.
/// </summary>
public interface IMongoCollectionInterceptor<T>
    where T : class, IValidatable, IBaseEntity, IRootEntity
{
    Task InsertOneAsync(T document, CancellationToken cancellationToken = default);
    Task<UpdateResult> UpdateOneAsync(FilterDefinition<T> filter, UpdateDefinition<T> update, CancellationToken cancellationToken = default);
    Task<DeleteResult> DeleteOneAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default);
    IFindFluent<T, T> Find(FilterDefinition<T> filter);
    Task<long> CountDocumentsAsync(FilterDefinition<T> filter,
        CountOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Wraps <see cref="IMongoCollection{T}"/> and invokes <see cref="IUnitOfWork.SaveChangesWithPlanAsync"/>
/// after inserts or updates.
/// </summary>
public class MongoCollectionInterceptor<T> : IMongoCollectionInterceptor<T>
    where T : class, IValidatable, IBaseEntity, IRootEntity
{
    private readonly IMongoCollection<T> _inner;
    private readonly IValidationService _validationService;

    public MongoCollectionInterceptor(IMongoDatabase database, IValidationService validationService)
    {
        _inner = database.GetCollection<T>(typeof(T).Name);
        _validationService = validationService;
    }

    public async Task InsertOneAsync(T document, CancellationToken cancellationToken = default)
    {
        await _inner.InsertOneAsync(document, cancellationToken: cancellationToken);
        await _validationService.ValidateAndSaveAsync(document, document.Id.ToString(), cancellationToken);
    }

    public async Task<UpdateResult> UpdateOneAsync(FilterDefinition<T> filter, UpdateDefinition<T> update, CancellationToken cancellationToken = default)
    {
        var result = await _inner.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        if (result.ModifiedCount > 0)
        {
            var updated = await _inner.Find(filter).FirstOrDefaultAsync(cancellationToken);
            if (updated != null)
            {
                await _validationService.ValidateAndSaveAsync(updated, updated.Id.ToString(), cancellationToken);
            }
        }
        return result;
    }

    public Task<DeleteResult> DeleteOneAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default)
        => _inner.DeleteOneAsync(filter, cancellationToken);

    public IFindFluent<T, T> Find(FilterDefinition<T> filter) => _inner.Find(filter);

    public Task<long> CountDocumentsAsync(FilterDefinition<T> filter,
        CountOptions? options = null,
        CancellationToken cancellationToken = default)
        => _inner.CountDocumentsAsync(filter, options, cancellationToken);
}
