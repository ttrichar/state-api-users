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
    public class EditAlbumRequest
    {
        [DataMember]
        public virtual UserAlbum Album { get; set; }
    }
    public static class EditAlbum
    {
        [FunctionName("EditAlbum")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            return await req.Manage<EditAlbumRequest, UsersState, UsersStateHarness>(log, async (mgr, reqData) =>
            {
                await mgr.EditAlbum(reqData.Album);

                return await mgr.WhenAll(
                );
            });
        }
    }
}