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
using System.Runtime.Serialization;
using System.Collections.Generic;
using AmblOn.State.API.Users.Graphs;
using Fathym;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using AmblOn.State.API.Users.State;
using Microsoft.Azure.Storage.Blob;
using LCU.StateAPI.Utilities;
using AmblOn.State.API.Itineraries.State;
using AmblOn.State.API.AmblOn.State;

namespace AmblOn.State.API.Users
{
    [DataContract]
    public class UnshareItinerariesRequest
    {
        [DataMember]
        public virtual List<Itinerary> Itineraries { get; set; }

        [DataMember]
        public virtual List<string> Usernames {get; set;}
    }

    public class UnshareItinerary
    {
        #region Fields
        protected AmblOnGraph amblGraph;
        #endregion

        #region Constructors
        public UnshareItinerary(AmblOnGraph amblGraph)
        {
            this.amblGraph = amblGraph;
        }
        #endregion

        [FunctionName("UnshareItineraries")]
        public virtual async Task<Status> Run([HttpTrigger(AuthorizationLevel.Admin)] HttpRequest req, ILogger log,
            [SignalR(HubName = AmblOnState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-lookup}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<ItinerariesState, UnshareItinerariesRequest, ItinerariesStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
                log.LogInformation($"UnshareItineraries");

                var stateDetails = StateUtils.LoadStateDetails(req);

                await harness.UnshareItineraries(amblGraph, stateDetails.EnterpriseLookup, reqData.Itineraries, reqData.Usernames);

                return Status.Success;
            });
        }
    }
}
