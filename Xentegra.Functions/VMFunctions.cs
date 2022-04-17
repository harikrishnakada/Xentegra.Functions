using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.Compute.Fluent;
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
        private readonly IAzure _azure;

        public VMFunctions(IAzure azure)
        {
            _azure = azure;
        }

        [FunctionName("GetAllVMByResourceGroupFunction")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetAllVMByResourceGroupFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "vm/resourceGroup/{rg}")] HttpRequest req, ILogger log, string rg)
        {
            try
            {

                var vms = new List<VirtualMaachine>();

                foreach (var virtualMachine in _azure.VirtualMachines.ListByResourceGroup(rg))
                {
                    log.LogInformation($"{virtualMachine.Name}");

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
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [FunctionName("GetVMById")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetVMById(
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = "vm/{vmId}")] HttpRequest req, ILogger log, string vmId)
        {
            try
            {
                var virtualMachine = await _azure.VirtualMachines.GetByIdAsync(vmId);
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
                return new OkObjectResult(vm);
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [FunctionName("ToggleVMState")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        [return: Queue("vm-queue", Connection = "CLOUD_STORAGE_CS")]
        public async Task<IActionResult> ToggleVMState(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "vm/status/{turnOn}")] VirtualMaachine vm, HttpRequest req, ILogger log, bool turnOn)
        {
            try
            {
                var virtualMachine = await _azure.VirtualMachines.GetByIdAsync(vm.id);

                if (virtualMachine == null)
                    throw new KeyNotFoundException("VM not found");

                var queueMessage = new
                {
                    vmId = vm.id,
                    turnOn = turnOn
                };

                return new OkObjectResult(queueMessage);
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                throw;
            }
        }

        [FunctionName("ToggleVMStateQueueTrigger")]
        public async Task ToggleVMStateQueueTrigger(
       [QueueTrigger("vm-queue", Connection = "CLOUD_STORAGE_CS")] dynamic queueItem,
       ILogger log)
        {
            try
            {
                log.LogInformation($"C# function processed: {queueItem}");
                string vmId = queueItem.Value?.vmId;
                bool turnOn = queueItem.Value.turnOn;

                IVirtualMachine virtualMachine = await _azure.VirtualMachines.GetByIdAsync(vmId);

                log.LogInformation($"Current status of the VM: {virtualMachine.PowerState.Value}");

                if (turnOn)
                {
                    log.LogInformation($"Turning on the VM: {virtualMachine.Name}");
                    await virtualMachine.StartAsync();
                    log.LogInformation($"Turned on the VM: {virtualMachine.Name}");
                }
                else
                {
                    log.LogInformation($"Turning off the VM: {virtualMachine.Name}");
                    await virtualMachine.DeallocateAsync();
                    log.LogInformation($"Turned off the VM: {virtualMachine.Name}");
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                throw;
            }
        }


    }
}

