using System;
using System.IO;
using System.Linq;
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
using Xentegra.Models.Constants;
using Xentegra.Models.DTO;

namespace Xentegra.Public.Functions
{
    public class DashboardFunction
    {
        private readonly IItemsContainer _itemsContainer;
        private readonly ILookupContainer _lookupContainer;

        public DashboardFunction(IItemsContainer itemsContainer, ILookupContainer lookupContainer)
        {
            _itemsContainer = itemsContainer;
            _lookupContainer = lookupContainer;
        }

        [FunctionName("CreateDemoRequest")]
        [OpenApiOperation(operationId: "CreateDemoRequest", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> CreateDemoRequest(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "public/dashboard/createDemoRequest")] DemoRequest demoRequest, HttpRequest req, ILogger log)
        {
            try
            {
                DemoRequest response;

                if (string.IsNullOrEmpty(demoRequest.technology?.id))
                {
                    log.LogError($"The technology is blank in the request");
                    return new BadRequestObjectResult($"The technology is blank in the request");
                }

                Technology technology = await this._itemsContainer.GetItem<Technology>(demoRequest.technology?.id, demoRequest.technology.pk);
                if (technology == null)
                {
                    log.LogError($"The technology with name {technology.name} does not exist");
                    return new BadRequestObjectResult($"The technology with name {technology.name} does not exist");
                }

                //set the partitionkey.
                demoRequest.SetPartitionKey();

                demoRequest.OnCreated();
                demoRequest.requestStatus = RequestStatus.Pending.ToString();

                //Only save the necessary information.
                demoRequest.technology = new()
                {
                    id = demoRequest.technology.id,
                    name = demoRequest.technology.name,
                };

                response = await this._itemsContainer.CreateItemAsync<DemoRequest>(demoRequest, demoRequest.pk);

                DemoRequestDTO demoRequestDTO = new()
                {
                    id = response.id,
                    name = response.name,
                    email = response.email,
                    requestType = response.requestType,
                    company = response.company,
                    phone = response.phone,
                    requestStatus = response.requestStatus,
                    technology = new()
                    {
                        id = technology.id,
                        name = technology.name,
                        resourceGroupName = technology.resourceGroupName,
                        pk = technology.pk
                    }
                };

                return new OkObjectResult(demoRequestDTO);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [FunctionName("GetAllTechnologies")]
        [OpenApiOperation(operationId: "GetAllTechnologies", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetAllTechnologies(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "public/dashboard/getAllTechnologies")] HttpRequest req, ILogger log)
        {
            var requestId = Guid.NewGuid().ToString();
            var items = await _lookupContainer.GetItems<Technology>(log: log, requestId: requestId);

            return new OkObjectResult(items.Where(x => x.entityType == typeof(Technology).ToString()));
        }

    }
}

