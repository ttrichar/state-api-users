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
using AmblOn.State.API.Users.Models;
using AmblOn.State.API.Users.Harness;
using System.Runtime.Serialization;

namespace AmblOn.State.API.Users
{
    public static class ImportLocations
    {

        [FunctionName("ImportLocations")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            return await req.Manage<dynamic, UsersState, UsersStateHarness>(log, async (mgr, reqData) =>
            {
                //var json = reqData.ToString();

                //var json = $"[{reqData.ToString()}]";
             
                string json = "[" +
"{    \"Icon\": \"Ferry\",    \"Title\": \"Waiheke Ferry Terminal\",    \"Website\": \"\",    \"Instagram\": \"\",    \"Address\": \"\",    \"Town\": \"Oneroa\",    \"State\": \"Auckland\",    \"Zipcode\": \"\",    \"Country\": \"New Zealand\",    \"Telephone\": \"\",    \"Region\": \"Oceania\",    \"Latitude\": -36.78065,    \"Longitude\": 174.99147  }," + 
 "{    \"Icon\": \"Hotel\",    \"Title\": \"The Boatshed\",    \"Website\": \"https://www.boatshed.co.nz/\",    \"Instagram\": \"@theboatshedhotel\",    \"Address\": \"Huia Street\",    \"Town\": \"Oneroa\",    \"State\": \"Auckland\",    \"Zipcode\": 1081,    \"Country\": \"New Zealand\",    \"Telephone\": \"+64 9 372 3242\",    \"Region\": \"Oceania\",    \"Latitude\": -36.78608,    \"Longitude\": 175.01766  }," + 
 "{    \"Icon\": \"Vineyard\",    \"Title\": \"Cable Bay\",    \"Website\": \"https://cablebay.nz/\",    \"Instagram\": \"@cable_bay_vineyards\",    \"Address\": \"12 Nick Johnstone Drive\",    \"Town\": \"Oneroa\",    \"State\": \"Auckland\",    \"Zipcode\": 1840,    \"Country\": \"New Zealand\",    \"Telephone\": \"+64 9 372 5889\",    \"Region\": \"Oceania\",    \"Latitude\": -36.78767,    \"Longitude\": 175.00036  }," + 
 "{    \"Icon\": \"Vineyard\",    \"Title\": \"Man O' War\",    \"Website\": \"http://www.manowarvineyards.co.nz/\",    \"Instagram\": \"@manowarwine\",    \"Address\": \"725 Man O' War Bay Road\",    \"Town\": \"Waiheke Island\",    \"State\": \"Auckland\",    \"Zipcode\": 1971,    \"Country\": \"New Zealand\",    \"Telephone\": \"+64 9 372 9678\",    \"Region\": \"Oceania\",    \"Latitude\": -36.786,    \"Longitude\": 175.15447  }," + 
 "{    \"Icon\": \"Vineyard\",    \"Title\": \"Mudbrick\",    \"Website\": \"https://www.mudbrick.co.nz/\",    \"Instagram\": \"@mudbrick_nz\",    \"Address\": \"126 Church Bay Road\",    \"Town\": \"Oneroa\",    \"State\": \"Auckland\",    \"Zipcode\": 1971,    \"Country\": \"New Zealand\",    \"Telephone\": \"+64 9 372 9050\",    \"Region\": \"Oceania\",    \"Latitude\": -36.79247,    \"Longitude\": 175.0001  }," + 

 "]";

                await mgr.LoadCuratedLocationsIntoDB(json, new Guid("a4d4b426-6684-4736-a231-7eac3ca140da"));
                // await mgr.LoadCuratedLocationsIntoDB(json, new Guid("4704a25b-049b-49a9-90b0-2551b40045c3"));

                return await mgr.WhenAll(
                );
            });

        }
    }
}
