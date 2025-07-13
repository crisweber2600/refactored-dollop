using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExampleData;

public interface IMongoDbService
{
    Task InsertManyItemsAsync<T>(IEnumerable<T> items, string collectionName);
}
