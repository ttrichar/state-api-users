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
    public class EditPhotoRequest
    {
        [DataMember]
        public virtual Guid AlbumID { get; set; }

        [DataMember]
        public virtual UserPhoto Photo { get; set; }
    }
    public static class EditPhoto
    {
        [FunctionName("EditPhoto")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            return await req.Manage<EditPhotoRequest, UsersState, UsersStateHarness>(log, async (mgr, reqData) =>
            {
                log.LogInformation($"Editing Photo for album: {reqData.AlbumID}");

                await mgr.EditPhoto(reqData.Photo, reqData.AlbumID);

                return await mgr.WhenAll(
                );
            });
        }
    }
}
