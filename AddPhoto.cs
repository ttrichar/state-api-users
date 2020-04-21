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
using LCU.Personas.Client.Enterprises;
using LCU.Personas.Client.Applications;

namespace AmblOn.State.API.Users
{
    [DataContract]
    public class AddPhotoRequest
    {
        [DataMember]
        public virtual Guid AlbumID { get; set; }

        [DataMember]
        public virtual UserPhoto Photo { get; set; }

        [DataMember]
        public virtual Guid LocationID { get; set; }
    }

    public class AddPhoto
    {
        #region Fields
        protected AmblOnGraph amblGraph;
        
        protected ApplicationManagerClient appMgr;
        
        protected EnterpriseManagerClient entMgr;
        #endregion

        #region Constructors
        public AddPhoto(AmblOnGraph amblGraph, EnterpriseManagerClient entMgr, ApplicationManagerClient appMgr)
        {
            this.amblGraph = amblGraph;
            
            this.appMgr = appMgr;
            
            this.entMgr = entMgr;
        }
        #endregion

        [FunctionName("AddPhoto")]
        public virtual async Task<Status> Run([HttpTrigger] HttpRequest req, ILogger log,
            [SignalR(HubName = UsersState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-api-key}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<UsersState, AddPhotoRequest, UsersStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
                log.LogInformation($"AddPhoto");

                var stateDetails = StateUtils.LoadStateDetails(req);

                await harness.AddPhoto(entMgr, appMgr, amblGraph, stateDetails.Username, stateDetails.EnterpriseAPIKey, stateDetails.ApplicationID, reqData.Photo, reqData.AlbumID, reqData.LocationID);

                return Status.Success;
            });
        }
    }
}
