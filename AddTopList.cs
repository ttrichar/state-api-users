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
    public class AddTopListRequest
    {
        [DataMember]
        public virtual UserTopList TopList { get; set; }
    }

    public static class AddTopList
    {
        [FunctionName("AddTopList")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try {
            return await req.Manage<AddTopListRequest, UsersState, UsersStateHarness>(log, async (mgr, reqData) =>
            {
                var reqResult = reqData;
                return await mgr.AddTopList(reqData.TopList);
            });
            } catch (Exception ex) { 
                throw ex;
            }
        }
    }
}