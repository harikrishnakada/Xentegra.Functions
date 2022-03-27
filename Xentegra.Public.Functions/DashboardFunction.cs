using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json;

using Xentegra.DataAccess.CosmosDB.Containers;
using Xentegra.Models;
using Xentegra.Models.DTO;

namespace Xentegra.Public.Functions
{
    public class DashboardFunction
    {
        private readonly IItemsContainer _itemsContainer;

        public DashboardFunction(IItemsContainer itemsContainer)
        {
            _itemsContainer = itemsContainer;
        }

        [FunctionName("UpsertDemoRequest")]
        [OpenApiOperation(operationId: "UpsertDemoRequest", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> UpsertDemoRequest(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] DemoRequest demoRequest, HttpRequest req, ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                if (string.IsNullOrEmpty(demoRequest.id))
                    demoRequest.OnCreated();
                else
                    demoRequest.OnChanged();

                if (string.IsNullOrEmpty(demoRequest.technology?.id))
                {
                    log.LogError($"The technology is blank in the request");
                    return new BadRequestObjectResult($"The technology is blank in the request");
                }

                var technology = await this._itemsContainer.GetItem<Technology>(demoRequest.technology?.id, demoRequest.technology?.GetPartitionKey());
                if (technology == null)
                {
                    log.LogError($"The technology with name {technology.name} does not exist");
                    return new BadRequestObjectResult($"The technology with name {technology.name} does not exist");
                }

                //set the partitionkey.
                demoRequest.SetPartitionKey();
                var response = await this._itemsContainer.UpsertItem<DemoRequest>(demoRequest, demoRequest.pk);

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [FunctionName("UpsertTechnology")]
        [OpenApiOperation(operationId: "UpsertTechnology", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> UpsertTechnology(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] Technology technology, HttpRequest req, ILogger log)
        {

            if (string.IsNullOrEmpty(technology.id))
                technology.OnCreated();
            else
                technology.OnChanged();

            //set the partitionkey.
            technology.SetPartitionKey();

            Technology response;
            try
            {
                response = await this._itemsContainer.UpsertItem<Technology>(technology, technology.pk);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

            return new OkObjectResult(response);
        }

    }
}

