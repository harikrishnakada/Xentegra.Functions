using Microsoft.Azure.Cosmos;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Xentegra.DataAccess.CosmosDB
{
    public class CosmosDBRepository : ICosmosDBRepository
    {
        private readonly CosmosClient _cosmosClient;
        private readonly TelemetryClient _telemetryClient;

        private Container _container { get; set; }

        public CosmosDBRepository(CosmosClient cosmosClient, TelemetryConfiguration configuration)
        {
            _cosmosClient = cosmosClient;
            _telemetryClient = new TelemetryClient(configuration);
        }

        public async Task<ItemResponse<T>> GetItem<T>(string id, string pk) where T : class
        {
            PartitionKey partitionKey = new(pk);

            var response = await _container.ReadItemAsync<T>(id, partitionKey);
            response.Diagnostics.GetClientElapsedTime();
            LogTelemetry<T>(OperationType.Read, response.RequestCharge, response.Diagnostics.GetClientElapsedTime());

            return response;
        }

        public virtual void SetContainer(string databaseName, string containerName)
        {
            this._container = _cosmosClient.GetContainer(databaseName, containerName);
        }

        public async Task<ItemResponse<T>> UpsertItem<T>(T item, string pk) where T : class
        {
            PartitionKey partitionKey = new(pk);
            var result = await _container.UpsertItemAsync<T>(item, partitionKey);
            return result;
        }

        public async Task<ItemResponse<T>> ReadAndUpsertItem<T>(string id, string pk, Func<T, Task<T>> UpdateItem) where T : class
        {
            ItemResponse<T> result = null;
            T item = await GetItem<T>(id, pk);
            if (item != null)
            {
                string eTag = (item as ItemResponse<T>).ETag;

                item = await UpdateItem(item);

                ItemRequestOptions options = new ItemRequestOptions { IfMatchEtag = eTag };
                PartitionKey partitionKey = new(pk);
                result = await _container.UpsertItemAsync<T>(item, partitionKey, requestOptions: options);
            }
            return result;
        } 
        
        public async Task<ItemResponse<T>> ReadAndUpsertItem<T>(string id, string pk, Func<T, T> UpdateItem) where T : class
        {
            ItemResponse<T> result = null;
            ItemResponse<T> itemResponse = await GetItem<T>(id, pk);
            if (itemResponse != null)
            {
                string eTag = itemResponse.ETag;
                var item = itemResponse.Resource;
                item = UpdateItem(item);

                ItemRequestOptions options = new ItemRequestOptions { IfMatchEtag = eTag };
                PartitionKey partitionKey = new(pk);
                result = await _container.UpsertItemAsync<T>(item, partitionKey, requestOptions: options);
            }
            return result;
        }

        private void LogTelemetry<T>(OperationType operationType, double requestCharge, TimeSpan elapsedTime)
        {
            _telemetryClient?.TrackEvent("Cosmos DB request", new Dictionary<string, string>()
            {
                {"EntityType", typeof(T).ToString() },
                {"OperationType", operationType.ToString() },
                {"RequestCharge", requestCharge.ToString() },
                {"ExecutionTime", elapsedTime.ToString() }
            });
        }

    }
}