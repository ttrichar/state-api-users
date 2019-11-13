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
    public class AddAlbumRequest
    {
        [DataMember]
        public virtual UserAlbum Album { get; set; }

        [DataMember]
        public virtual List<ImageMessage> Images {get; set;}
    }
    public static class AddAlbum
    {
        [FunctionName("AddAlbum")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            return await req.Manage<AddAlbumRequest, UsersState, UsersStateHarness>(log, async (mgr, reqData) =>
            {
                log.LogInformation($"Adding Album");

                await mgr.AddAlbum(reqData.Album, reqData.Images);

                return await mgr.WhenAll(
                );
            });
        }
    }
}
