using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using AutoMapper;

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
using Newtonsoft.Json.Linq;

using Xentegra.DataAccess.CosmosDB.Containers;
using Xentegra.Models;
using Xentegra.Models.Constants;
using Xentegra.Models.DTO;

namespace Xentegra.Functions
{
    public class DashboardFunction
    {
        private readonly IItemsContainer _itemsContainer;
        private readonly ILookupContainer _lookupContainer;

        private readonly IMapper _mapper;

        public DashboardFunction(IItemsContainer itemsContainer, ILookupContainer lookupContainer, IMapper mapper)
        {
            _itemsContainer = itemsContainer;
            _lookupContainer = lookupContainer;
            _mapper = mapper;
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
                var requestId = Guid.NewGuid().ToString();

                DemoRequest response;

                if (string.IsNullOrEmpty(demoRequest.technology?.id))
                {
                    log.LogError($"The technology is blank in the request");
                    return new BadRequestObjectResult($"The technology is blank in the request");
                }

                Technology technology = await this._lookupContainer.GetItem<Technology>(demoRequest.technology.id, demoRequest.technology?.pk, log, requestId: requestId);
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

                    response = await this._itemsContainer.CreateItemAsync<DemoRequest>(demoRequest, demoRequest.pk, log, requestId: requestId);
                }
                else
                {
                    DemoRequest SetEntity(DemoRequest item)
                    {
                        item.SetEntity(demoRequest);
                        return item;
                    }
                    response = await this._itemsContainer.ReadAndUpsertItem<DemoRequest>(demoRequest.id, demoRequest.pk, SetEntity, log, requestId: requestId);
                }

                DemoRequestDTO demoRequestDTO = this._mapper.Map<DemoRequestDTO>(response);
                demoRequestDTO.technology = this._mapper.Map<TechnologyDTO>(technology);

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
            var requestId = Guid.NewGuid().ToString();
            IEnumerable<dynamic> demoRequestItems = await _itemsContainer.GetItems<ExpandoObject>(log: log, requestId: requestId);
            IEnumerable<Technology> technologyItems = await _lookupContainer.GetItems<Technology>(log: log, requestId: requestId);

            var demoRequests = this._mapper.Map<IEnumerable<DemoRequestDTO>>(demoRequestItems.Where(x => x.entityType == typeof(DemoRequest).ToString()));
            var technologioes = this._mapper.Map<IEnumerable<TechnologyDTO>>(technologyItems.Where(x => x.entityType == typeof(Technology).ToString()));

            var demoRequestsDto = from dr in demoRequests
                                  join t in technologioes on dr.technology?.id equals t.id
                                  select new
                                  {
                                      dr,
                                      t
                                  };
            var demoRequestList = new List<object>();
            foreach (var item in demoRequestsDto)
            {
                item.dr.technology = item.t;
                var objToken = JToken.FromObject(item.dr);
                var obj = objToken
    .SelectTokens("$..*")
    .Where(t => !t.HasValues)
    .ToDictionary(t => t.Path, t => t.ToString());
                demoRequestList.Add(obj);
            }

            return new OkObjectResult(demoRequestList);
        }

        [FunctionName("UpsertTechnology")]
        [OpenApiOperation(operationId: "UpsertTechnology", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> UpsertTechnology(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "dashboard/upsertTechnology")] Technology technology, HttpRequest req, ILogger log)
        {
            var requestId = Guid.NewGuid().ToString();

            //set the partitionkey.
            technology.SetPartitionKey();

            Technology response;
            try
            {
                if (string.IsNullOrEmpty(technology.id))
                {
                    technology.OnCreated();
                    response = await this._itemsContainer.CreateItemAsync<Technology>(technology, technology.pk, log, requestId: requestId);
                }
                else
                {
                    Technology SetEntity(Technology item)
                    {
                        item.SetEntity(technology);
                        return item;
                    }

                    response = await this._itemsContainer.ReadAndUpsertItem<Technology>(technology.id, technology.pk, SetEntity, log, requestId: requestId);
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
            var requestId = Guid.NewGuid().ToString();
            List<Task<ItemResponse<Technology>>> taskList = new();
            List<Technology> output;
            try
            {
                foreach (Technology technology in technologies)
                {
                    //set the partitionkey.
                    technology.SetPartitionKey();
                    technology.OnCreated();
                    taskList.Add(this._lookupContainer.CreateItemAsync<Technology>(technology, technology.pk, log, requestId: requestId));
                }

                var result = await Task.WhenAll(taskList);
                output = result.Select(x => x.Resource).ToList();
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

            return new OkObjectResult(output);
        }
    }
}

