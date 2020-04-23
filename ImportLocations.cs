using System;
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
using Fathym;using Microsoft.Azure.WebJobs.Extensions.SignalRService;using AmblOn.State.API.Users.State;using Microsoft.WindowsAzure.Storage.Blob;using LCU.StateAPI.Utilities;
using System.Runtime.Serialization;
using AmblOn.State.API.Users.Graphs;

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
            [SignalR(HubName = UsersState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-api-key}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<UsersState, ImportLocationsRequest, UsersStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
                log.LogInformation($"ImportLocations");

                var stateDetails = StateUtils.LoadStateDetails(req);

                await harness.LoadCuratedLocationsIntoDB(amblGraph, stateDetails.Username, stateDetails.EnterpriseAPIKey, 
                    reqData.LocationImportJSON, reqData.AccoladeList, new Guid(reqData.LayerID));

                return Status.Success;
            });
        }
    }
}
