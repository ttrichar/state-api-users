using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AmblOn.State.API.Users.Models;
using Fathym;using Microsoft.Azure.WebJobs.Extensions.SignalRService;using AmblOn.State.API.Users.State;using Microsoft.Azure.Storage.Blob;using LCU.StateAPI.Utilities;
using AmblOn.State.API.AmblOn.State;

namespace AmblOn.State.API.Users
{
    public class LoadLocationsFromJSON
    {
        [Disable]
        [FunctionName("LoadLocationsFromJSON")]
        public virtual async Task<Status> Run([HttpTrigger(AuthorizationLevel.Admin)] HttpRequest req, ILogger log,
            [SignalR(HubName = AmblOnState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-lookup}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<UsersState, RemoveSelectedLayerRequest, UsersStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
                log.LogInformation($"LoadLocationsFromJSON");

                var stateDetails = StateUtils.LoadStateDetails(req);

                //await harness.LoadCuratedLocationsIntoDB("moxhay@gmail.com", json, new Guid("4704a25b-049b-49a9-90b0-2551b40045c3"));

                return Status.Success;
            });
        }
    }
}
