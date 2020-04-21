using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Security.Claims;
using LCU.StateAPI;
using AmblOn.State.API.Users.State;

namespace LCU.State.API.NapkinIDE.InfrastructureManagement.Host
{
    public class ConnectToState
    {
        [FunctionName("ConnectToState")]
        public virtual async Task<ConnectToStateResponse> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req, ILogger logger, 
            ClaimsPrincipal claimsPrincipal, [DurableClient] IDurableEntityClient entity,
            [SignalR(HubName = UsersState.HUB_NAME)]IAsyncCollector<SignalRGroupAction> signalRGroupActions)
        {
            return await entity.ConnectToState<UsersState>(req, logger, claimsPrincipal, signalRGroupActions);
        }
    }
}
