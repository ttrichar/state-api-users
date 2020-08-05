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
            return await stateBlob.WithStateHarness<UsersState, RefreshRequest, UsersStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
                log.LogInformation($"Refresh");

                var stateDetails = StateUtils.LoadStateDetails(req);

                //await harness.Load(amblGraph, stateDetails.Username, stateDetails.EnterpriseAPIKey);

                await harness.Refresh(amblGraph, amblGraphFactory, stateDetails.Username, stateDetails.EnterpriseAPIKey);
                
                return Status.Success;
            });
        }
    }
}
