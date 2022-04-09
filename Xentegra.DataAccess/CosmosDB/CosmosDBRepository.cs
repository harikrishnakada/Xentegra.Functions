using Microsoft.Azure.Cosmos;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos.Linq;
using System.Collections;
using System.Collections.Generic;

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

        public async Task<ItemResponse<T>> GetItem<T>(string id, string pk, ILogger? log = null, string? requestId = null) where T : class
        {
            PartitionKey partitionKey = new(pk);

            var response = await _container.ReadItemAsync<T>(id, partitionKey);
            response.Diagnostics.GetClientElapsedTime();
            LogTelemetry<T>(OperationType.Read, response.RequestCharge, response.Diagnostics.GetClientElapsedTime().TotalMilliseconds, log, requestId: requestId);

            return response;
        }

        public async Task<IEnumerable<T>> GetItems<T>(string? pk = null, ILogger? log = null, string? requestId = null) where T : class
        {

            List<T> result = new();
            double totalMilliseconds = 0;
            double requestCharge = 0;

            QueryRequestOptions queryRequestOptions = new()
            {
                MaxItemCount = 100
            };

            if (!string.IsNullOrEmpty(pk))
                queryRequestOptions.PartitionKey = new PartitionKey(pk);

            using (FeedIterator<T> feedIterator = _container.GetItemLinqQueryable<T>(true, requestOptions: queryRequestOptions)
                           .ToFeedIterator())
            {
                while (feedIterator.HasMoreResults)
                {
                    var currentPage = await feedIterator.ReadNextAsync();
                    requestCharge += currentPage.RequestCharge;
                    totalMilliseconds += currentPage.Diagnostics.GetClientElapsedTime().TotalMilliseconds;
                    result.AddRange(currentPage.Resource);
                }
            }

            LogTelemetry<T>(OperationType.ReadAll, requestCharge, totalMilliseconds, log, requestId: requestId);

            return result;
        }

        public virtual void SetContainer(string databaseName, string containerName)
        {
            this._container = _cosmosClient.GetContainer(databaseName, containerName);
        }

        public async Task<ItemResponse<T>> UpsertItem<T>(T item, string pk, ILogger? log = null, string? requestId = null) where T : class
        {
            PartitionKey partitionKey = new(pk);
            var result = await _container.UpsertItemAsync<T>(item, partitionKey);

            LogTelemetry<T>(OperationType.Upsert, result, log, requestId: requestId);

            return result;
        }

        public async Task<ItemResponse<T>> CreateItemAsync<T>(T item, string pk, ILogger? log = null, string? requestId = null) where T : class
        {
            PartitionKey partitionKey = new(pk);
            var result = await _container.CreateItemAsync<T>(item, partitionKey);

            LogTelemetry<T>(OperationType.Add, result, log, requestId: requestId);

            return result;
        }

        public async Task<ItemResponse<T>> ReadAndUpsertItem<T>(string id, string pk, Func<T, Task<T>> UpdateItem, ILogger? log = null, string? requestId = null) where T : class
        {
            requestId = requestId ?? Guid.NewGuid().ToString();
            ItemResponse<T> result = null;
            ItemResponse<T> itemResponse = await GetItem<T>(id, pk);

            if (itemResponse != null)
            {
                string eTag = itemResponse.ETag;
                var item = itemResponse.Resource;
                item = await UpdateItem(item);

                ItemRequestOptions options = new ItemRequestOptions { IfMatchEtag = eTag };
                PartitionKey partitionKey = new(pk);
                result = await _container.UpsertItemAsync<T>(item, partitionKey, requestOptions: options);

                LogTelemetry<T>(OperationType.Upsert, result, log, requestId: requestId);
            }
            return result;
        }

        public async Task<ItemResponse<T>> ReadAndUpsertItem<T>(string id, string pk, Func<T, T> UpdateItem, ILogger? log = null, string? requestId = null) where T : class
        {
            requestId = requestId ?? Guid.NewGuid().ToString();
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

                LogTelemetry<T>(OperationType.Upsert, result, log, requestId: requestId);
            }
            return result;
        }

        private void LogTelemetry<T>(OperationType operationType, double requestCharge, double totalMilliseconds, ILogger log, string? requestId = null)
        {
            requestId = requestId ?? Guid.NewGuid().ToString();
            var logDict = new Dictionary<string, string>()
            {
                {"RequestId", requestId },
                {"EntityType", typeof(T).ToString() },
                {"OperationType", operationType.ToString() },
                {"RequestCharge", requestCharge.ToString() },
                {"ExecutionTime", totalMilliseconds.ToString() }
            };

            if (log != null)
            {
                foreach (var item in logDict)
                    log.LogInformation($"{item.Key}: {item.Value}");
            }
            _telemetryClient?.TrackEvent("Cosmos DB request", logDict);

        }

        private void LogTelemetry<T>(OperationType operationType, ItemResponse<T> response, ILogger log, string? requestId = null)
        {
            requestId = requestId ?? Guid.NewGuid().ToString();
            var logDict = new Dictionary<string, string>()
            {
                {"RequestId", requestId },
                {"EntityType", typeof(T).ToString() },
                {"OperationType", operationType.ToString() },
                {"RequestCharge", response.RequestCharge.ToString() },
                {"ExecutionTime", response.Diagnostics.GetClientElapsedTime().TotalMilliseconds.ToString() }
            };

            if (log != null)
            {
                foreach (var item in logDict)
                    log.LogInformation($"{item.Key}: {item.Value}");
            }
            _telemetryClient?.TrackEvent("Cosmos DB request", logDict);
        }
    }
}