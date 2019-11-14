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
using System.Drawing;
using LCU.Presentation;

namespace AmblOn.State.API.Users
{
    [DataContract]
    public class EditAccoladeRequest
    {
        [DataMember]
        public virtual UserAccolade Accolade { get; set; }

        [DataMember]
        public virtual Guid LocationID { get; set; }
    }

    public static class EditAccolade
    {
        [FunctionName("EditAccolade")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            return await req.Manage<AddAccoladeRequest, UsersState, UsersStateHarness>(log, async (mgr, reqData) =>
            {
                log.LogInformation($"Editing Accolade");

                await mgr.EditAccolade(reqData.Accolade, reqData.LocationID);

                return await mgr.WhenAll(
                );
            });
        }
    }
}
