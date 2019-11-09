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
    public class DeleteAccoladeRequest
    {
        [DataMember]
        public virtual Guid[] AccoladeIDs { get; set; }

        [DataMember]
        public virtual Guid LocationID { get; set; }
    }
    public static class DeleteAccolade
    {
        [FunctionName("DeleteAccolade")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            return await req.Manage<DeleteAccoladeRequest, UsersState, UsersStateHarness>(log, async (mgr, reqData) =>
            {
                await mgr.DeleteAccolades(reqData.AccoladeIDs, reqData.LocationID);

                return await mgr.WhenAll(
                );
            });
        }
    }
}
