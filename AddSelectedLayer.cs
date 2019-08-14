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
using AmblOn.State.API.Users.Harness;
using Microsoft.WindowsAzure.Storage;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace AmblOn.State.API.Users
{
    [DataContract]
    public class AddSelectedLayerRequest
    {
        [DataMember]
        public virtual Guid LayerID { get; set; }
    }
    public static class AddSelectedLayer
    {
        [FunctionName("AddSelectedLayer")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            return await req.Manage<AddSelectedLayerRequest, UsersState, UsersStateHarness>(log, async (mgr, reqData) =>
            {
                await mgr.AddSelectedLayer(reqData.LayerID);

                return await mgr.WhenAll(
                );
            });
        }
    }
}
