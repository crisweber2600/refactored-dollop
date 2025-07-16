using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace WorkerService1.Repositories
{
    /// <summary>
    /// MongoDB repository implementation showing how to integrate ExampleLib.Infrastructure.ValidationRunner.
    /// This demonstrates the pattern for adding validation to existing MongoDB repositories.
    /// </summary>
    public class MongoRepository<T> : IRepository<T> where T : class, IValidatable, IBaseEntity, IRootEntity
    {
        private readonly IMongoCollection<T> _collection;
        private readonly IValidationRunner _validationRunner;

        public MongoRepository(IMongoClient mongoClient, IValidationRunner validationRunner, string dbName, string collectionName)
        {
            var database = mongoClient.GetDatabase(dbName);
            _collection = database.GetCollection<T>(collectionName);
            _validationRunner = validationRunner;
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            var filter = Builders<T>.Filter.Eq("Id", id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<List<T>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            // Auto-increment int Id for MongoDB if Id is 0
            var idProp = typeof(T).GetProperty("Id");
            if (idProp != null && idProp.PropertyType == typeof(int))
            {
                int idValue = (int)idProp.GetValue(entity)!;
                if (idValue == 0)
                {
                    int nextId = await GetNextIdAsync();
                    idProp.SetValue(entity, nextId);
                }
            }

            // INTEGRATION POINT: Use ExampleLib ValidationRunner for comprehensive validation
            // This includes manual validation, summarisation validation, and sequence validation
            var isValid = await _validationRunner.ValidateAsync(entity);
            entity.Validated = isValid;

            await _collection.InsertOneAsync(entity);
        }

        private async Task<int> GetNextIdAsync()
        {
            var db = _collection.Database;
            var counters = db.GetCollection<MongoCounter>("counters");
            var filter = Builders<MongoCounter>.Filter.Eq(c => c.CollectionName, _collection.CollectionNamespace.CollectionName);
            var update = Builders<MongoCounter>.Update.Inc(c => c.Seq, 1);
            var options = new FindOneAndUpdateOptions<MongoCounter> { IsUpsert = true, ReturnDocument = ReturnDocument.After };
            var counter = await counters.FindOneAndUpdateAsync(filter, update, options);
            return counter.Seq;
        }

        private class MongoCounter
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string? Id { get; set; }
            public string CollectionName { get; set; } = string.Empty;
            public int Seq { get; set; }
        }

        public async Task UpdateAsync(T entity)
        {
            // Run validation before updating
            var isValid = await _validationRunner.ValidateAsync(entity);
            entity.Validated = isValid;

            var idProp = typeof(T).GetProperty("Id");
            if (idProp == null) return;
            var id = idProp.GetValue(entity);
            var filter = Builders<T>.Filter.Eq("Id", id);
            await _collection.ReplaceOneAsync(filter, entity);
        }

        public async Task DeleteAsync(int id)
        {
            var filter = Builders<T>.Filter.Eq("Id", id);
            await _collection.DeleteOneAsync(filter);
        }

        public async Task<bool> ValidateAsync(T entity, CancellationToken cancellationToken = default)
        {
            // INTEGRATION POINT: Expose ValidationRunner functionality to repository consumers
            return await _validationRunner.ValidateAsync(entity, cancellationToken);
        }

        public async Task<T?> GetLastAsync()
        {
            var sort = Builders<T>.Sort.Descending("Id");
            return await _collection.Find(_ => true).Sort(sort).FirstOrDefaultAsync();
        }
    }
}