using MongoDB.Driver;
using WorkerService1.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExampleLib.Domain;
using System.Threading;

namespace WorkerService1.Repositories
{
    public class MongoSampleRepository : ISampleRepository<SampleEntity>
    {
        private readonly IMongoCollection<SampleEntity> _collection;
        private readonly IValidationRunner _validationRunner;
        public MongoSampleRepository(IMongoClient mongoClient, IValidationRunner validationRunner)
        {
            var database = mongoClient.GetDatabase("SampleEntities"); // Use the correct database name if different
            _collection = database.GetCollection<SampleEntity>("SampleEntities");
            _validationRunner = validationRunner;
        }

        public async Task<SampleEntity?> GetByIdAsync(int id)
        {
            var filter = Builders<SampleEntity>.Filter.Eq(e => e.Id, id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<List<SampleEntity>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task AddAsync(SampleEntity entity)
        {
            var isValid = await _validationRunner.ValidateAsync(entity, CancellationToken.None);
            if (isValid)
            {
                await _collection.InsertOneAsync(entity);
            }
            // else: do not insert if not valid
        }

        public async Task UpdateAsync(SampleEntity entity)
        {
            var isValid = await _validationRunner.ValidateAsync(entity, CancellationToken.None);
            if (isValid)
            {
                var filter = Builders<SampleEntity>.Filter.Eq(e => e.Id, entity.Id);
                await _collection.ReplaceOneAsync(filter, entity);
            }
            // else: do not update if not valid
        }

        public async Task DeleteAsync(int id)
        {
            var filter = Builders<SampleEntity>.Filter.Eq(e => e.Id, id);
            await _collection.DeleteOneAsync(filter);
        }
    }
}