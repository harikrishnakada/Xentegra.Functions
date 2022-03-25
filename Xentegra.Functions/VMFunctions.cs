using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json;

namespace Xentegra.Functions
{
    public class VMFunctions
    {
        private readonly ILogger<VMFunctions> _logger;
        private readonly IAzure _azure;
        private readonly GraphServiceClient _graphServiceClient;

        public VMFunctions(ILogger<VMFunctions> log, IAzure azure, GraphServiceClient graphServiceClient)
        {
            _logger = log;
            _azure = azure;
            _graphServiceClient = graphServiceClient;
        }

        [FunctionName("VMFunctions")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string resourceGroupName = req.Query["rg"];

            var status = req.Query["status"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            resourceGroupName = resourceGroupName ?? data?.resourceGroupName;

            var vms = new List<object>();

            foreach (var virtualMachine in _azure.VirtualMachines.ListByResourceGroup(resourceGroupName))
            {
                if (status == "true")
                    await _azure.VirtualMachines.StartAsync(resourceGroupName, virtualMachine.Name);
                if (status == "false")
                    await _azure.VirtualMachines.DeallocateAsync(resourceGroupName, virtualMachine.Name);

                _logger.LogInformation($"{virtualMachine.Name}");
                var obj = new { virtualMachine.Name, virtualMachine.OSType, virtualMachine.PowerState };
                vms.Add(obj);

            }
            return new OkObjectResult(vms);
        }

      
    }
}

