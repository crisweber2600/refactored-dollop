using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using ExampleLib.Domain;

namespace WorkerService1.Repositories
{
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
            await _collection.InsertOneAsync(entity);
        }

        public async Task UpdateAsync(T entity)
        {
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
            return await _validationRunner.ValidateAsync(entity, cancellationToken);
        }
    }
}