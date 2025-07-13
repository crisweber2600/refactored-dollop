using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExampleData;

public class MongoDbService : IMongoDbService
{
    private readonly IMongoDatabase _database;

    public MongoDbService(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public async Task InsertManyItemsAsync<T>(IEnumerable<T> items, string collectionName)
    {
        var collection = _database.GetCollection<T>(collectionName);
        await collection.InsertManyAsync(items);
    }
}
