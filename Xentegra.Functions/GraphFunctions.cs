using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Graph;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using System.Net;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Xentegra.Models.DTO;
using System.Linq;

namespace Xentegra.Functions
{
    public class GraphFunctions
    {
        private readonly GraphServiceClient _graphClient;

        public GraphFunctions(GraphServiceClient graphServiceClient)
        {
            _graphClient = graphServiceClient;
        }

        [FunctionName("AddUserToGroup")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> AddUserToGroup(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                GraphUserDTO data = JsonConvert.DeserializeObject<GraphUserDTO>(requestBody);
                User user;
                try
                {
                    user = await _graphClient.Users[data.UserId ?? data.UserPrincipalName].Request().GetAsync();
                }
                catch (Exception ex)
                {
                    log.LogError($"User with Id/UserPrincipalName : {data.UserId}/{data.UserPrincipalName} does not exist.");
                    throw;
                }

                foreach (var item in data.Groups)
                {
                    if (string.IsNullOrEmpty(item.GroupId) && string.IsNullOrEmpty(item.GroupName))
                        continue;
                    if (string.IsNullOrEmpty(item.GroupId))
                    {
                        var group = await _graphClient.Groups.Request()
                                         .Filter($"displayName eq '{item.GroupName}'")
                                         .GetAsync();

                        if (group == null || !group.Any())
                        {
                            log.LogError($"Group with Id/Name : {item.GroupId}/{item.GroupName} does not exist");
                            continue;
                        }

                        item.GroupId = group.FirstOrDefault()?.Id;
                    }
                    else
                    {
                        try
                        {
                            var group = await _graphClient.Groups[item.GroupId].Request()
                                           .GetAsync();
                        }
                        catch (Exception ex)
                        {
                            log.LogError($"Group with Id/Name : {item.GroupId}/{item.GroupName} does not exist");
                            item.GroupId = null;
                            continue;
                        }
                    }
                }

                var groups = data.Groups.Where(x => !String.IsNullOrEmpty(x.GroupId));

                var directoryObject = new DirectoryObject
                {
                    Id = user.Id
                };

                var groupMembersTaskList = groups.Select(x =>
                {
                    return _graphClient.Groups[x.GroupId].Members.References
                    .Request()
                    .AddAsync(directoryObject).ContinueWith(t =>
                    {
                        if (t.Status == System.Threading.Tasks.TaskStatus.Faulted)
                            log.LogError(t.Exception, $"Failed to add user {user.UserPrincipalName} to group {x.GroupName}/{x.GroupName}");
                    });
                });

                await Task.WhenAll(groupMembersTaskList);

                return new OkObjectResult(new { });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }
        }
    }
}