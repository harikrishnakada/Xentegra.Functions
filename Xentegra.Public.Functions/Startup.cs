using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xentegra.DataAccess.CosmosDB.Containers;
using Xentegra.Extensions;

[assembly: FunctionsStartup(typeof(Xentegra.Public.Functions.Startup))]

namespace Xentegra.Public.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            //var credentials = new DefaultAzureCredential();
            var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");

            var cosmos_cs = Environment.GetEnvironmentVariable("COSMOSDB_CS");

            CosmosClient cosmosClient = new(cosmos_cs);

            builder.Services.AddSingleton(cosmosClient);
            builder.Services.AddSingleton<IItemsContainer, ItemsContainer>();
            builder.Services.AddSingleton<ILookupContainer, LookupContainer>();

            builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
        }
    }
}
