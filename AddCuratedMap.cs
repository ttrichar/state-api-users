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
using AmblOn.State.API.Users.Harness;

namespace AmblOn.State.API.Users
{
    [DataContract]
    public class AddCuratedMapRequest
    {

    }
    
    public static class AddCuratedMap
    {
        [FunctionName("AddCuratedMap")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            return await req.Manage<AddCuratedMapRequest, UsersState, UsersStateHarness>(log, async (mgr, reqData) =>
            {
                return await mgr.AddCuratedMap();
            });
        }
    }
}
