using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Cosmos;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xentegra.DataAccess.CosmosDB.Containers
{
    public class ItemsContainer : CosmosDBRepository, IItemsContainer
    {
        public readonly string _databaseName = "xentegra-db";
        public readonly string _containerName = "items";

        public ItemsContainer(CosmosClient cosmosClient, TelemetryConfiguration configuration) : base(cosmosClient, configuration)
        {
            base.SetContainer(_databaseName, _containerName);
        }
    }

    public interface IItemsContainer : ICosmosDBRepository
    {
        //Add the functions specics to the container like thrughput, ttl.
    }
}
