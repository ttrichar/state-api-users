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

namespace AmblOn.State.API.Users
{
    public static class LoadLocationsFromJSON
    {
        [FunctionName("LoadLocationsFromJSON")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            return await req.Manage<dynamic, UsersState, UsersStateHarness>(log, async (mgr, reqData) =>
            {
                var json = String.Empty;
                
                //await mgr.LoadCuratedLocationsIntoDB("moxhay@gmail.com", json, new Guid("4704a25b-049b-49a9-90b0-2551b40045c3"));

                return await mgr.WhenAll(
                );
            });
        }
    }
}
