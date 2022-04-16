using System;
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
using Xentegra.Models.Constants;

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

            var vms = new List<VirtualMaachine>();

            foreach (var virtualMachine in _azure.VirtualMachines.ListByResourceGroup(rg))
            {
                _logger.LogInformation($"{virtualMachine.Name}");

                var obj = new { virtualMachine.Id, virtualMachine.Name, virtualMachine.OSType, virtualMachine.PowerState };
                VirtualMaachine vm = new()
                {
                    id = virtualMachine.Id,
                    name = virtualMachine.Name,
                    osType = virtualMachine.OSType.ToString(),
                    powerState = virtualMachine.PowerState.Value,
                    resourceGroupName = virtualMachine.ResourceGroupName,
                    isTurnedOn = virtualMachine.PowerState.Value switch
                    {
                        VMPowerStateCode.Allocated => true,
                        VMPowerStateCode.Deallocated => false,
                        _ => false
                    }
                };
                vms.Add(vm);
            }
            return new OkObjectResult(vms);
        }

        [FunctionName("ToggleVMFunction")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        [return: Queue("vm-queue", Connection = "CLOUD_STORAGE_CS")]
        public async Task<IActionResult> ToggleVMFunction(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "vm/status/{turnOn}")] VirtualMaachine vm, HttpRequest req, ILogger log, bool turnOn)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var virtualMachine = _azure.VirtualMachines.GetById(vm.id);

            //if (turnOn)
            //    await virtualMachine.StartAsync();
            //else
            //    await virtualMachine.DeallocateAsync();

            var queueMessage = new
            {
                vmId = vm.id,
                turnOn = turnOn
            };

            return new OkObjectResult(queueMessage);
        }

        [FunctionName("QueueTrigger")]
        public async Task QueueTrigger(
       [QueueTrigger("vm-queue", Connection = "CLOUD_STORAGE_CS") ] dynamic queueItem,
       ILogger log)
        {
            try
            {
                log.LogInformation($"C# function processed: {queueItem}");

                var virtualMachine = _azure.VirtualMachines.GetById(queueItem.vmId);

                if (queueItem.turnOn)
                    await virtualMachine.StartAsync();
                else
                    await virtualMachine.DeallocateAsync();
            }catch(Exception ex)
            {
                log.LogError(ex, ex.Message);
            }
        }


    }
}

