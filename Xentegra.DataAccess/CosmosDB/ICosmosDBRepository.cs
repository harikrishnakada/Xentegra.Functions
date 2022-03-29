
using Microsoft.Azure.Cosmos;

namespace Xentegra.DataAccess.CosmosDB
{
    public interface ICosmosDBRepository
    {
        void SetContainer(string databaseName, string containerName);
        Task<ItemResponse<T>> GetItem<T>(string id, string pk) where T : class;
        Task<ItemResponse<T>> UpsertItem<T>(T item, string pk) where T : class;

        Task<ItemResponse<T>> ReadAndUpsertItem<T>(string id, string pk, Func<T, Task<T>> UpdateItem) where T : class;
        Task<ItemResponse<T>> ReadAndUpsertItem<T>(string id, string pk, Func<T,T> UpdateItem) where T : class;

    }
}