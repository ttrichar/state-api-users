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
using Microsoft.WindowsAzure.Storage;
using System.Runtime.Serialization;
using System.Collections.Generic;
using AmblOn.State.API.Users.Graphs;
using static AmblOn.State.API.Users.Host.Startup;
using AmblOn.State.API.Itineraries.State;
using AmblOn.State.API.AmblOn.State;

namespace AmblOn.State.API.Users
{
    [DataContract]
    public class ItineraryItemOrderAdjustedRequest
    {
        [DataMember]
        public virtual Itinerary Itinerary { get; set; }    

        [DataMember]
        public virtual Guid? ActivityChanged { get; set; }
    }

    public class ItineraryItemOrderAdjusted
    {
        #region Fields
        protected AmblOnGraph amblGraph;
        protected AmblOnGraphFactory amblGraphFactory;

        #endregion

        #region Constructors
        public ItineraryItemOrderAdjusted(AmblOnGraph amblGraph, AmblOnGraphFactory amblGraphFactory)
        {
            this.amblGraph = amblGraph; 

            this.amblGraphFactory = amblGraphFactory;
        }
        #endregion

        [FunctionName("ItineraryItemOrderAdjusted")]
        public virtual async Task<Status> Run([HttpTrigger(AuthorizationLevel.Admin)] HttpRequest req, ILogger log,
            [SignalR(HubName = AmblOnState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-lookup}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<ItinerariesState, ItineraryItemOrderAdjustedRequest, ItinerariesStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
                log.LogInformation($"ItineraryItemOrderAdjusted");

                var stateDetails = StateUtils.LoadStateDetails(req);

                await harness.ItineraryItemOrderAdjusted(amblGraph, stateDetails.Username, stateDetails.EnterpriseLookup, reqData.Itinerary, reqData.ActivityChanged);

                return Status.Success;
            });
        }
    }
}
