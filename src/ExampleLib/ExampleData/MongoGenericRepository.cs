using MongoDB.Driver;
using ExampleData.Infrastructure;
using ExampleLib.Domain;

using System.Linq;
namespace ExampleData;

public class MongoGenericRepository<T> : IGenericRepository<T>
    where T : class, IValidatable, IBaseEntity, IRootEntity
{
    private readonly IMongoCollectionInterceptor<T> _collection;
    private readonly IBatchValidationService _batchValidator;

    public MongoGenericRepository(IMongoCollectionInterceptor<T> collection, IBatchValidationService batchValidator)
    {
        _collection = collection;
        _batchValidator = batchValidator;
    }

    public MongoGenericRepository(IMongoDatabase database, IValidationService validationService, IBatchValidationService batchValidator)
        : this(new MongoCollectionInterceptor<T>(database, validationService), batchValidator)
    {
    }

    public async Task<T?> GetByIdAsync(int id, bool includeDeleted = false)
    {
        var filter = Builders<T>.Filter.Eq(e => e.Id, id);
        if (!includeDeleted)
            filter &= Builders<T>.Filter.Eq(e => e.Validated, true);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<List<T>> GetAllAsync()
    {
        var filter = Builders<T>.Filter.Eq(e => e.Validated, true);
        return await _collection.Find(filter).ToListAsync();
    }

    public Task AddAsync(T entity)
    {
        return _collection.InsertOneAsync(entity);
    }

    public async Task AddManyAsync(IEnumerable<T> entities)
    {
        var list = entities.ToList();
        foreach (var entity in list)
            await _collection.InsertOneAsync(entity);
        _batchValidator.ValidateAndAudit<T>(list.Count);
    }

    public Task UpdateAsync(T entity)
    {
        var filter = Builders<T>.Filter.Eq(e => e.Id, entity.Id);
        return _collection.ReplaceOneAsync(filter, entity);
    }

    public async Task UpdateManyAsync(IEnumerable<T> entities)
    {
        foreach (var entity in entities)
        {
            var filter = Builders<T>.Filter.Eq(e => e.Id, entity.Id);
            await _collection.ReplaceOneAsync(filter, entity);
        }
    }

    public async Task DeleteAsync(T entity, bool hardDelete = false)
    {
        var filter = Builders<T>.Filter.Eq(e => e.Id, entity.Id);
        if (hardDelete)
        {
            await _collection.DeleteOneAsync(filter);
        }
        else
        {
            var update = Builders<T>.Update.Set(e => e.Validated, false);
            await _collection.UpdateOneAsync(filter, update);
        }
    }

    public async Task<int> CountAsync()
    {
        var filter = Builders<T>.Filter.Eq(e => e.Validated, true);
        var count = await _collection.CountDocumentsAsync(filter);
        return (int)count;
    }
}
