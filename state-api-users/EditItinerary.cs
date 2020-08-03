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
using Fathym;using Microsoft.Azure.WebJobs.Extensions.SignalRService;using AmblOn.State.API.Users.State;using Microsoft.WindowsAzure.Storage.Blob;using LCU.StateAPI.Utilities;
using Microsoft.WindowsAzure.Storage;
using System.Runtime.Serialization;
using System.Collections.Generic;
using AmblOn.State.API.Users.Graphs;
using static AmblOn.State.API.Users.Host.Startup;

namespace AmblOn.State.API.Users
{
    [DataContract]
    public class EditItineraryRequest
    {
        [DataMember]
        public virtual Itinerary Itinerary { get; set; }

        [DataMember]
        public virtual List<ActivityLocationLookup> ActivityLocationLookups { get; set; }
    }

    public class EditItinerary
    {
        #region Fields
        protected AmblOnGraph amblGraph;
        protected AmblOnGraphFactory amblGraphFactory;

        #endregion

        #region Constructors
        public EditItinerary(AmblOnGraph amblGraph, AmblOnGraphFactory amblGraphFactory)
        {
            this.amblGraph = amblGraph; 

            this.amblGraphFactory = amblGraphFactory;
        }
        #endregion

        [FunctionName("EditItinerary")]
        public virtual async Task<Status> Run([HttpTrigger(AuthorizationLevel.Admin)] HttpRequest req, ILogger log,
            [SignalR(HubName = UsersState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-api-key}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<UsersState, EditItineraryRequest, UsersStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
                log.LogInformation($"EditItinerary");

                var stateDetails = StateUtils.LoadStateDetails(req);

                await harness.EditItinerary(amblGraph, amblGraphFactory, stateDetails.Username, stateDetails.EnterpriseAPIKey, reqData.Itinerary, reqData.ActivityLocationLookups);

                return Status.Success;
            });
        }
    }
}
