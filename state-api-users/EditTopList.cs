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
using AmblOn.State.API.AmblOn.State;
using LCU.Presentation.State.ReqRes;
using AmblOn.State.API.Locations.State;

namespace AmblOn.State.API.Users
{
    [DataContract]
    public class EditTopListRequest
    {
        [DataMember]
        public virtual TopList TopList { get; set; }

        [DataMember]
        public virtual List<ActivityLocationLookup> ActivityLocationLookups { get; set; }
    }

    public class EditTopList
    {
        #region Fields
        protected AmblOnGraph amblGraph;
        #endregion

        #region Constructors
        public EditTopList(AmblOnGraph amblGraph)
        {
            this.amblGraph = amblGraph;
        }
        #endregion

        [FunctionName("EditTopList")]
        public virtual async Task<Status> Run([HttpTrigger(AuthorizationLevel.Admin)] HttpRequest req, ILogger log,
            [SignalR(HubName = AmblOnState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-lookup}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob,
            [Blob("state-api/{headers.lcu-ent-lookup}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/locations", FileAccess.ReadWrite)] CloudBlockBlob locationStateBlob)
            {
            var status = await stateBlob.WithStateHarness<UsersState, EditTopListRequest, UsersStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
                log.LogInformation($"EditTopList");

                var stateDetails = StateUtils.LoadStateDetails(req);

                var username = stateDetails.Username;

                await harness.EditTopList(amblGraph, stateDetails.Username, stateDetails.EnterpriseLookup, reqData.TopList);

                var locationStateDetails = StateUtils.LoadStateDetails(req);

                locationStateDetails.StateKey = "locations";

                var exActReq = await req.LoadBody<ExecuteActionRequest>();

                return await locationStateBlob.WithStateHarness<LocationsState, EditTopListRequest, LocationsStateHarness>(locationStateDetails, exActReq, signalRMessages, log,
                    async (newharness, reqData, actReq) =>
                {
                    log.LogInformation($"EditTopList Location Refresh");

                    await newharness.RefreshLocations(amblGraph, locationStateDetails.EnterpriseLookup, username);

                    return Status.Success;
                });                 
            });

            return status;
        }
    }
}
