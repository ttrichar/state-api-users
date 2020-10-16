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
using Microsoft.Azure.Storage.Blob;
using LCU.StateAPI.Utilities;
using AmblOn.State.API.AmblOn.State;
using AmblOn.State.API.Itineraries.State;
using AmblOn.State.API.Locations.State;

namespace AmblOn.State.API.Users.Host
{
    public class ConnectToState
    {
        [FunctionName("ConnectToState")]
        public static async Task<ConnectToStateResponse> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req, ILogger log,
            ClaimsPrincipal claimsPrincipal, //[LCUStateDetails]StateDetails stateDetails,
            [SignalR(HubName = AmblOnState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [SignalR(HubName = AmblOnState.HUB_NAME)]IAsyncCollector<SignalRGroupAction> signalRGroupActions,
            [Blob("state-api/{headers.lcu-ent-lookup}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            var stateDetails = StateUtils.LoadStateDetails(req);

            if (stateDetails.StateKey == "users")
                return await signalRMessages.ConnectToState<UsersState>(req, log, claimsPrincipal, stateBlob, signalRGroupActions);
            else if (stateDetails.StateKey == "itineraries")
                return await signalRMessages.ConnectToState<ItinerariesState>(req, log, claimsPrincipal, stateBlob, signalRGroupActions);
            else if (stateDetails.StateKey == "locations")
                return await signalRMessages.ConnectToState<LocationsState>(req, log, claimsPrincipal, stateBlob, signalRGroupActions);
            else
                throw new Exception("A valid State Key must be provided (amblon, itineraries, locations).");
        }
    }
}
