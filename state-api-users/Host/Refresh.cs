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
using Microsoft.WindowsAzure.Storage;
using Fathym;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using AmblOn.State.API.Users.State;
using Microsoft.WindowsAzure.Storage.Blob;
using Fathym.API;
using System.Runtime.Serialization;
using LCU.StateAPI.Utilities;
using AmblOn.State.API.Users.Graphs;
using static AmblOn.State.API.Users.Host.Startup;
using AmblOn.State.API.AmblOn.State;
using AmblOn.State.API.Itineraries.State;
using AmblOn.State.API.Locations.State;

namespace AmblOn.State.API.Users
{
    [Serializable]
    [DataContract]
    public class RefreshRequest : BaseRequest
    { }

    public class Refresh
    {
        #region Fields
        protected AmblOnGraph amblGraph;

        protected AmblOnGraphFactory amblGraphFactory;
        #endregion

        #region Constructors
        public Refresh(AmblOnGraph amblGraph, AmblOnGraphFactory amblGraphFactory)
        {
            this.amblGraph = amblGraph;

            this.amblGraphFactory = amblGraphFactory;
        }
        #endregion

        [FunctionName("Refresh")]
        public virtual async Task<Status> Run([HttpTrigger(AuthorizationLevel.Admin)] HttpRequest req, ILogger log,
            [SignalR(HubName = UsersState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-api-key}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            var stateDetails = StateUtils.LoadStateDetails(req);

            if (stateDetails.StateKey == "amblon")
                return await stateBlob.WithStateHarness<AmblOnState, RefreshRequest, AmblOnStateHarness>(req, signalRMessages, log,
                    async (harness, refreshReq, actReq) =>
                {
                    log.LogInformation($"Refreshing AmblOn state");

                    return await refreshAmblOn(harness, log, stateDetails);
                });
            else if (stateDetails.StateKey == "itineraries")
                return await stateBlob.WithStateHarness<ItinerariesState, RefreshRequest, ItinerariesStateHarness>(req, signalRMessages, log,
                    async (harness, refreshReq, actReq) =>
                {
                    log.LogInformation($"Refreshing itineraries state");

                    return await refreshItineraries(harness, log, stateDetails);
                });
            else if (stateDetails.StateKey == "locations")
                return await stateBlob.WithStateHarness<LocationsState, RefreshRequest, LocationsStateHarness>(req, signalRMessages, log,
                    async (harness, refreshReq, actReq) =>
                {
                    log.LogInformation($"Refreshing locations state");

                    return await refreshLocations(harness, log, stateDetails);
                });
            else
                throw new Exception("A valid State Key must be provided (amblon, itineraries, locations).");
        }

        #region Helpers
        protected virtual async Task<Status> refreshAmblOn(AmblOnStateHarness harness, ILogger log, StateDetails stateDetails)
        {
            await harness.RefreshAmblOn(amblGraph, amblGraphFactory, stateDetails.EnterpriseAPIKey, stateDetails.Username);

            return Status.Success;
        }

        protected virtual async Task<Status> refreshItineraries(ItinerariesStateHarness harness, ILogger log, StateDetails stateDetails)
        {
            await harness.RefreshItineraries(amblGraph, amblGraphFactory, stateDetails.EnterpriseAPIKey, stateDetails.Username);

            return Status.Success;
        }

        protected virtual async Task<Status> refreshLocations(LocationsStateHarness harness, ILogger log, StateDetails stateDetails)
        {
            await harness.RefreshLocations(amblGraph, amblGraphFactory, stateDetails.EnterpriseAPIKey, stateDetails.Username);

            return Status.Success;
        }
        #endregion
    }
}
