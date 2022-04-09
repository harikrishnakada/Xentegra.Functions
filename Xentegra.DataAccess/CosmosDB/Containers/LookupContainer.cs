using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Cosmos;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xentegra.DataAccess.CosmosDB.Containers
{
    public class LookupContainer : CosmosDBRepository, ILookupContainer
    {
        public readonly string _databaseName = "xentegra-db";
        public readonly string _containerName = "lookups";
        public readonly string _partitionKey = "/pk";

        private readonly CosmosClient _cosmosClient;
        public ContainerProperties CosmosContainer { get; set; }

        public LookupContainer(CosmosClient cosmosClient, TelemetryConfiguration configuration) : base(cosmosClient, configuration)
        {
            base.SetContainer(_databaseName, _containerName);
            _cosmosClient = cosmosClient;
            CosmosContainer = Task.Run(async () => await this.CreateConatinerAsync()).Result;
        }

        public async Task<ContainerProperties> CreateConatinerAsync()
        {
            var containerDef = new ContainerProperties
            {
                Id = _containerName,
                PartitionKeyPath = _partitionKey,
            };

            var database = this._cosmosClient.GetDatabase(_databaseName);
            var result = await database.CreateContainerIfNotExistsAsync(containerDef);
            return result.Resource;
        }
    }

    public interface ILookupContainer : ICosmosDBRepository, ICosmosContainer
    {
        //Add the functions specific to the container like thrughput, ttl.

    }
}
