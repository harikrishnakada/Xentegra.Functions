using System;
using System.Collections.Generic;
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

namespace Xentegra.Functions
{
    public class DashboardFunction
    {
        private readonly IItemsContainer _itemsContainer;

        public DashboardFunction(IItemsContainer itemsContainer)
        {
            _itemsContainer = itemsContainer;
        }

        [FunctionName("UpsertDemoRequest")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> UpsertDemoRequest(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "dashboard/upsertDemoRequest")] DemoRequest demoRequest, HttpRequest req, ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                DemoRequest response;

                if (string.IsNullOrEmpty(demoRequest.technology?.id))
                {
                    log.LogError($"The technology is blank in the request");
                    return new BadRequestObjectResult($"The technology is blank in the request");
                }

                Technology technology = await this._itemsContainer.GetItem<Technology>(demoRequest.technology.id, demoRequest.technology?.GetPartitionKey());
                if (technology == null)
                {
                    log.LogError($"The technology with name {technology.name} does not exist");
                    return new BadRequestObjectResult($"The technology with name {technology.name} does not exist");
                }

                //set the partitionkey.
                demoRequest.SetPartitionKey();

                if (string.IsNullOrEmpty(demoRequest.id))
                {
                    demoRequest.OnCreated();
                    demoRequest.requestStatus = RequestStatus.Pending.ToString();

                    //Only save the necessary information.
                    demoRequest.technology = new()
                    {
                        id = demoRequest.technology.id,
                        name = demoRequest.technology.name,
                    };

                    response = await this._itemsContainer.CreateItemAsync<DemoRequest>(demoRequest, demoRequest.pk);
                }
                else
                {
                    DemoRequest SetEntity(DemoRequest item)
                    {
                        item.SetEntity(demoRequest);
                        return item;
                    }
                    response = await this._itemsContainer.ReadAndUpsertItem<DemoRequest>(demoRequest.id, demoRequest.pk, SetEntity);
                }

                DemoRequestDTO demoRequestDTO = new()
                {
                    id = response.id,
                    name = response.name,
                    email = response.email,
                    requestType = response.requestType,
                    company = response.company,
                    requestStatus = response.requestStatus,
                    phone = response.phone,
                    technology = new()
                    {
                        id = technology.id,
                        name = technology.name,
                        resourceGroupName = technology.resourceGroupName
                    }
                };

                return new OkObjectResult(demoRequestDTO);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        //TODO: Update API to return resource group name.
        [FunctionName("GetAllDemoRequests")]
        [OpenApiOperation(operationId: "GetAllDemoRequests", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetAllDemoRequests(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "dashboard/getAllDemoRequests")] HttpRequest req, ILogger log)
        {
            var items = await _itemsContainer.GetItems<DemoRequest>(log: log);

            var demoRequests = items.Where(x => x.enityType == typeof(DemoRequest).ToString());

            return new OkObjectResult(demoRequests);
        }

        [FunctionName("UpsertTechnology")]
        [OpenApiOperation(operationId: "UpsertTechnology", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> UpsertTechnology(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "dashboard/upsertTechnology")] Technology technology, HttpRequest req, ILogger log)
        {
            //set the partitionkey.
            technology.SetPartitionKey();

            Technology response;
            try
            {
                if (string.IsNullOrEmpty(technology.id))
                {
                    technology.OnCreated();
                    response = await this._itemsContainer.CreateItemAsync<Technology>(technology, technology.pk);
                }
                else
                {
                    Technology SetEntity(Technology item)
                    {
                        item.SetEntity(technology);
                        return item;
                    }

                    response = await this._itemsContainer.ReadAndUpsertItem<Technology>(technology.id, technology.pk, SetEntity);
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

            return new OkObjectResult(response);
        }

        

        [FunctionName("UpsertTechnologyBulk")]
        [OpenApiOperation(operationId: "UpsertTechnology", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> UpsertTechnologyBulk(
       [HttpTrigger(AuthorizationLevel.Function, "post", Route = "dashboard/createTechnologies/bulk")] List<Technology> technologies, HttpRequest req, ILogger log)
        {
            List<Task<ItemResponse<Technology>>> taskList = new();
            List<Technology> output;
            try
            {
                foreach (Technology technology in technologies)
                {
                    //set the partitionkey.
                    technology.SetPartitionKey();
                    technology.OnCreated();
                    taskList.Add(this._itemsContainer.CreateItemAsync<Technology>(technology, technology.pk));
                }

                var result = await Task.WhenAll(taskList);
                output = result.Select(x=>x.Resource).ToList();
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

            return new OkObjectResult(output);
        }
    }
}

