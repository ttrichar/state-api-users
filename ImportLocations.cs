using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using AmblOn.State.API.Users.Models;
using AmblOn.State.API.Users.Harness;
using System.Runtime.Serialization;

namespace AmblOn.State.API.Users
{
    public static class ImportLocations
    {
        public class ImportLocationsRequest
        {
            [DataMember]
            public virtual string OwnerEmail { get; set; }

            [DataMember]
            public virtual string LayerID { get; set; }
    
            [DataMember]
            public virtual List<dynamic> LocationImportJSON { get; set;}

            [DataMember]
            public virtual List<string> AccoladeList {get; set;}
        }

        [FunctionName("ImportLocations")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = null)] HttpRequest req,
            ILogger log)
        {            
            return await req.Manage<ImportLocationsRequest, UsersState, UsersStateHarness>(log, async (mgr, reqData) =>
            {
                await mgr.LoadCuratedLocationsIntoDB(reqData.OwnerEmail, reqData.LocationImportJSON, reqData.AccoladeList, new Guid(reqData.LayerID));

                return await mgr.WhenAll(
                );
            });

        }
    }
}
