
namespace Xentegra.DataAccess.CosmosDB
{
    public interface ICosmosDBRepository
    {
        void SetContainer(string databaseName, string containerName);
        Task<T> GetItem<T>(string id, string pk) where T : class;
        Task<T> UpsertItem<T>(T item, string pk) where T : class;
    }
}