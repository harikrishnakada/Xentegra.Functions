using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;

//using Azure.Identity;

using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

using Xentegra.DataAccess.CosmosDB.Containers;

[assembly: FunctionsStartup(typeof(Xentegra.Functions.Startup))]

namespace Xentegra.Functions
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

            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);

            var azure = Microsoft.Azure.Management.Fluent.Azure
                      .Configure()
                      .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                      .Authenticate(credentials)
                      .WithSubscription(subscriptionId);

            var graphClient = Task.Run(async () => await GetGraphApiClient()).Result;

            builder.Services.AddSingleton<IAzure>((s) => { return azure; });
            builder.Services.AddSingleton<GraphServiceClient>((s) => { return graphClient; });


            var cosmos_cs = Environment.GetEnvironmentVariable("COSMOSDB_CS");
            CosmosClient cosmosClient = new(cosmos_cs);

            builder.Services.AddSingleton(cosmosClient);
            builder.Services.AddSingleton<IItemsContainer, ItemsContainer>();
        }

        private static async Task<GraphServiceClient> GetGraphApiClient()
        {
            var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            var clientId = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("GRAPH_CLIENT_SECRET");

            var domain = Environment.GetEnvironmentVariable("AZURE_DOMAIN_NAME"); ;

            var credentials = new ClientCredential(clientId, clientSecret);
            var authContext = new AuthenticationContext($"https://login.microsoftonline.com/{domain}/");
            var token = await authContext.AcquireTokenAsync("https://graph.microsoft.com/", credentials);
            var accessToken = token.AccessToken;

            var graphServiceClient = new GraphServiceClient(
                new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage
                .Headers
                .Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                    return Task.CompletedTask;
                }));

            return graphServiceClient;
        }
    }
}
