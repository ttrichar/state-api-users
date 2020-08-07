using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using AmblOn.State.API.Users.Models;
using Fathym;using Microsoft.Azure.WebJobs.Extensions.SignalRService;using AmblOn.State.API.Users.State;using Microsoft.WindowsAzure.Storage.Blob;using LCU.StateAPI.Utilities;
using AmblOn.State.API.Users.Graphs;

namespace AmblOn.State.API.Users
{
    [DataContract]
    public class ChangeExcludedCurationsRequest
    {
        [DataMember]
        public virtual string LocationIDs { get; set; }
    }

    public class ChangeExcludedCurations
    {
        #region Fields
        protected AmblOnGraph amblGraph;
        #endregion

        #region Constructors
        public ChangeExcludedCurations(AmblOnGraph amblGraph)
        {
            this.amblGraph = amblGraph;
        }
        #endregion

        [FunctionName("ChangeExcludedCurations")]
        public virtual async Task<Status> Run([HttpTrigger(AuthorizationLevel.Admin)] HttpRequest req, ILogger log,
            [SignalR(HubName = UsersState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-api-key}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<UsersState, ChangeExcludedCurationsRequest, UsersStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
                log.LogInformation($"ChangeExcludedCurations");

                var stateDetails = StateUtils.LoadStateDetails(req);

                var curationList = new ExcludedCurations()
                {
                    LocationIDs = reqData.LocationIDs
                };

                //await harness.ChangeExcludedCurations(amblGraph, stateDetails.Username, stateDetails.EnterpriseAPIKey, curationList);

                return Status.Success;
            });
        }
    }
}
