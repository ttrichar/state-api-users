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
using LCU.Personas.Client.Applications;

namespace AmblOn.State.API.Users
{
    [DataContract]
    public class SendInvitesRequest
    {
        [DataMember]
        public virtual List<string> Emails {get; set;}
    }

    public class SendInvite
    {
        #region Fields
        protected ApplicationManagerClient appMgr;
        #endregion

        #region Constructors
        public SendInvite(ApplicationManagerClient appMgr)
        {
            this.appMgr = appMgr;
        }
        #endregion

        [FunctionName("SendInvites")]
        public virtual async Task<Status> Run([HttpTrigger(AuthorizationLevel.Admin)] HttpRequest req, ILogger log,
            [SignalR(HubName = UsersState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-api-key}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<UsersState, SendInvitesRequest, UsersStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
                log.LogInformation($"SendInvites");

                var stateDetails = StateUtils.LoadStateDetails(req);

                await harness.SendInvites(appMgr, stateDetails.EnterpriseAPIKey, reqData.Emails);

                return Status.Success;
            });
        }
    }
}
