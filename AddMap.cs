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
    public class AddMapRequest
    {
        [DataMember]
        public virtual List<Location> Locations {get; set;}

        [DataMember]
        public virtual Map Map { get; set; }
    }
    public static class AddMap
    {
        [FunctionName("AddMap")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            return await req.Manage<AddMapRequest, UsersState, UsersStateHarness>(log, async (mgr, reqData) =>
            {
                await mgr.AddMap(reqData.Map, reqData.Locations);

                return await mgr.WhenAll(
                );
            });
        }
    }
}
