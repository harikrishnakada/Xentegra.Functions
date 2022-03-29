
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Xentegra.DataAccess.CosmosDB
{
    public interface ICosmosDBRepository
    {
        void SetContainer(string databaseName, string containerName);
        Task<ItemResponse<T>> GetItem<T>(string id, string pk, ILogger? log = null) where T : class;
        Task<IEnumerable<T>> GetItems<T>(string? pk=null, ILogger? log = null) where T : class;
        Task<ItemResponse<T>> UpsertItem<T>(T item, string pk, ILogger? log = null) where T : class;
        Task<ItemResponse<T>> CreateItemAsync<T>(T item, string pk, ILogger? log = null) where T : class;

        Task<ItemResponse<T>> ReadAndUpsertItem<T>(string id, string pk, Func<T, Task<T>> UpdateItem, ILogger? log = null) where T : class;
        Task<ItemResponse<T>> ReadAndUpsertItem<T>(string id, string pk, Func<T, T> UpdateItem, ILogger? log = null) where T : class;

    }
}