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
using System.Drawing;
using LCU.Presentation;
using AmblOn.State.API.Users.Graphs;
using LCU.Personas.Client.Enterprises;
using LCU.Personas.Client.Applications;
using AmblOn.State.API.AmblOn.State;

namespace AmblOn.State.API.Users
{
    [DataContract]
    public class AddAlbumRequest
    {
        [DataMember]
        public virtual UserAlbum Album { get; set; }

        [DataMember]
        public virtual List<ImageMessage> Images {get; set;}
    }

    public class AddAlbum
    {
        #region Fields
        protected AmblOnGraph amblGraph;
        
        protected ApplicationManagerClient appMgr;
        
        protected EnterpriseManagerClient entMgr;
        #endregion

        #region Constructors
        public AddAlbum(AmblOnGraph amblGraph, EnterpriseManagerClient entMgr, ApplicationManagerClient appMgr)
        {
            this.amblGraph = amblGraph;
            
            this.appMgr = appMgr;
            
            this.entMgr = entMgr;
        }
        #endregion

        [FunctionName("AddAlbum")]
        public virtual async Task<Status> Run([HttpTrigger(AuthorizationLevel.Admin)] HttpRequest req, ILogger log,
            [SignalR(HubName = AmblOnState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-api-key}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<UsersState, AddAlbumRequest, UsersStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
                log.LogInformation($"AddAlbum");

                var stateDetails = StateUtils.LoadStateDetails(req);

                await harness.AddAlbum(entMgr, appMgr, amblGraph, stateDetails.Username, stateDetails.EnterpriseAPIKey, stateDetails.ApplicationID, reqData.Album, reqData.Images);

                return Status.Success;
            });
        }
    }
}
