﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using AmblOn.State.API.Users.Models;
using Fathym;using Microsoft.Azure.WebJobs.Extensions.SignalRService;using AmblOn.State.API.Users.State;using Microsoft.Azure.Storage.Blob;using LCU.StateAPI.Utilities;
using System.Runtime.Serialization;
using AmblOn.State.API.Users.Graphs;
using AmblOn.State.API.Locations.State;
using AmblOn.State.API.AmblOn.State;

namespace AmblOn.State.API.Users
{
        public class ImportLocationsRequest
        {
            [DataMember]
            public virtual string OwnerEmail { get; set; }

            [DataMember]
            public virtual string LayerID { get; set; }
    
            [DataMember]
            public virtual List<dynamic> LocationImportJSON { get; set;}

            [DataMember]
            public virtual List<string> AccoladeList {get; set;}
        }

    public class ImportLocations
    {
        #region Fields
        protected AmblOnGraph amblGraph;
        #endregion

        #region Constructors
        public ImportLocations(AmblOnGraph amblGraph)
        {
            this.amblGraph = amblGraph;
        }
        #endregion

        [FunctionName("ImportLocations")]
        public virtual async Task<Status> Run([HttpTrigger(AuthorizationLevel.Admin)] HttpRequest req, ILogger log,
            [SignalR(HubName = AmblOnState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-lookup}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<LocationsState, ImportLocationsRequest, LocationsStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
                log.LogInformation($"ImportLocations");

                var stateDetails = StateUtils.LoadStateDetails(req);

                // await harness.LoadCuratedLocationsIntoDB(amblGraph, stateDetails.Username, stateDetails.EnterpriseLookup, 
                //     reqData.LocationImportJSON, reqData.AccoladeList, new Guid(reqData.LayerID));

                return Status.Success;
            });
        }
    }
}
