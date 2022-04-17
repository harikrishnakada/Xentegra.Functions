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
using Xentegra.Models.Constants;
using Xentegra.Models.Graph;
using System.Collections.Generic;

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
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "graph/groupMembers")] GraphUserDTO data, HttpRequest req, ILogger log)
        {
            try
            {
                User user;
                try
                {
                    user = await _graphClient.Users[data.userId ?? data.userPrincipalName].Request().GetAsync();
                }
                catch (Exception ex)
                {
                    log.LogError($"User with Id/UserPrincipalName : {data.userId}/{data.userPrincipalName} does not exist.");
                    throw;
                }

                foreach (var item in data.groups)
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

                var groups = data.groups.Where(x => !String.IsNullOrEmpty(x.GroupId));

                var directoryObject = new DirectoryObject
                {
                    Id = user.Id
                };

                var groupMembersTaskList = groups.Select(x =>
                {
                    if (data.action == OperationTypes.Add)
                        return _graphClient.Groups[x.GroupId].Members.References
                        .Request()
                        .AddAsync(directoryObject).ContinueWith(t =>
                        {
                            if (t.Status == System.Threading.Tasks.TaskStatus.Faulted)
                                log.LogError(t.Exception, $"Failed to add user {user.UserPrincipalName} to group {x.GroupName}/{x.GroupName}");
                        });
                    else if (data.action == OperationTypes.Delete)
                        return _graphClient.Groups[x.GroupId].Members[directoryObject.Id].Reference
                       .Request()
                       .DeleteAsync();
                    else
                        return Task.CompletedTask;
                });

                await Task.WhenAll(groupMembersTaskList);

                return new OkObjectResult(new { });
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }
        }

        [FunctionName("GetGroupMembers")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetGroupMembers(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "graph/group/{groupName}/groupMembers")] HttpRequest req, ILogger log, string groupName)
        {
            try
            {
                var group = await _graphClient.Groups.Request()
                                        .Filter($"displayName eq '{groupName}'")
                                        .GetAsync();

                if (group == null || !group.Any())
                {
                    log.LogError($"Group with Id/Name : {groupName} does not exist");
                    return new BadRequestObjectResult($"Group with Id/Name : {groupName} does not exist");
                }

                var members = await _graphClient.Groups[group.FirstOrDefault()?.Id].Members
                                    .Request()
                                    .GetAsync();

                IList<GraphUser> graphUserList = new List<GraphUser>();
                foreach (User user in members.OfType<User>())
                {
                    GraphUser graphUser = new()
                    {
                        id = user.Id,
                        name = user.DisplayName,
                        userPrincipalName = user.UserPrincipalName
                    };

                    graphUserList.Add(graphUser);
                }

                return new OkObjectResult(graphUserList);

            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
