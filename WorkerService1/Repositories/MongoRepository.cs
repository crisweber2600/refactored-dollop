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

        /// <summary>
        /// Add multiple entities with bulk validation using ValidationRunner.ValidateManyAsync.
        /// This is more efficient than validating each entity individually.
        /// </summary>
        public async Task AddManyAsync(IEnumerable<T> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();
            if (entityList.Count == 0)
                return;

            // Auto-increment int Id for MongoDB if Id is 0
            var idProp = typeof(T).GetProperty("Id");
            if (idProp != null && idProp.PropertyType == typeof(int))
            {
                foreach (var entity in entityList)
                {
                    int idValue = (int)idProp.GetValue(entity)!;
                    if (idValue == 0)
                    {
                        int nextId = await GetNextIdAsync();
                        idProp.SetValue(entity, nextId);
                    }
                }
            }

            // INTEGRATION POINT: Use ExampleLib ValidationRunner for bulk validation
            // This includes manual validation, summarisation validation, and sequence validation for all entities
            var isValid = await _validationRunner.ValidateManyAsync(entityList);

            // Set validation status for all entities
            foreach (var entity in entityList)
            {
                entity.Validated = isValid;
            }

            await _collection.InsertManyAsync(entityList);
        }

        public async Task UpdateAsync(T entity)
        {
            // Run validation before updating
            var isValid = await _validationRunner.ValidateAsync(entity);
            entity.Validated = isValid;

            var filter = Builders<T>.Filter.Eq("Id", entity.Id);
            await _collection.ReplaceOneAsync(filter, entity);
        }

        /// <summary>
        /// Update multiple entities with bulk validation using ValidationRunner.ValidateManyAsync.
        /// This is more efficient than validating each entity individually.
        /// </summary>
        public async Task UpdateManyAsync(IEnumerable<T> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();
            if (entityList.Count == 0)
                return;

            // INTEGRATION POINT: Use ExampleLib ValidationRunner for bulk validation
            // This includes manual validation, summarisation validation, and sequence validation for all entities
            var isValid = await _validationRunner.ValidateManyAsync(entityList);

            // Set validation status for all entities
            foreach (var entity in entityList)
            {
                entity.Validated = isValid;
            }

            // MongoDB doesn't have a direct bulk replace, so we do individual replaces
            var updates = new List<Task>();
            foreach (var entity in entityList)
            {
                var filter = Builders<T>.Filter.Eq("Id", entity.Id);
                updates.Add(_collection.ReplaceOneAsync(filter, entity));
            }

            await Task.WhenAll(updates);
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

        /// <summary>
        /// Validate multiple entities using bulk validation.
        /// This is more efficient than validating each entity individually.
        /// </summary>
        public async Task<bool> ValidateManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            // INTEGRATION POINT: Expose ValidationRunner bulk functionality to repository consumers
            return await _validationRunner.ValidateManyAsync(entities, cancellationToken);
        }

        public async Task<T?> GetLastAsync()
        {
            var sort = Builders<T>.Sort.Descending("Id");
            return await _collection.Find(_ => true).Sort(sort).FirstOrDefaultAsync();
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
    }
}