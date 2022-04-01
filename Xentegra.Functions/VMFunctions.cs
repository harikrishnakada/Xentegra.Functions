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

using Xentegra.Models;

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

        [FunctionName("GetAllVMByResourceGroupFunction")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetAllVMByResourceGroupFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "vm/{rg}")] HttpRequest req, ILogger log, string rg)
        {
            log.LogInformation("log C# HTTP trigger function processed a request.");

            var vms = new List<object>();

            foreach (var virtualMachine in _azure.VirtualMachines.ListByResourceGroup(rg))
            {
                _logger.LogInformation($"{virtualMachine.Name}");
                var obj = new { virtualMachine.Id, virtualMachine.Name, virtualMachine.OSType, virtualMachine.PowerState };
                vms.Add(obj);
            }
            return new OkObjectResult(vms);
        }

        [FunctionName("ToggleVMFunction")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> ToggleVMFunction(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "vm/status/{turnOn}")] VirtualMaachine vm, HttpRequest req, ILogger log, bool turnOn)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var virtualMachine = _azure.VirtualMachines.GetById(vm.id);

            if (turnOn)
                await virtualMachine.StartAsync();
            else
                await virtualMachine.DeallocateAsync();

            return new OkObjectResult(true);
        }


    }
}

