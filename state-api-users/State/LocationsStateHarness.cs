using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Fathym;
using LCU.Presentation.State.ReqRes;
using LCU.StateAPI.Utilities;
using LCU.StateAPI;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using System.Collections.Generic;
using LCU.Personas.Client.Enterprises;
using LCU.Personas.Client.DevOps;
using LCU.Personas.Enterprises;
using LCU.Personas.Client.Applications;
using LCU.Personas.Client.Identity;
using Fathym.API;
using LCU.Personas.Client.Security;
using AmblOn.State.API.Users.Models;
using LCU.Presentation;
using AmblOn.State.API.Users.Graphs;
using Microsoft.AspNetCore.WebUtilities;
using System.Device.Location;
using Newtonsoft.Json.Linq;
using static AmblOn.State.API.Users.Host.Startup;

namespace AmblOn.State.API.Locations.State
{
    public class LocationsStateHarness : LCUStateHarness<LocationsState>
    {
        #region Constants
        #endregion

        #region Fields 
        #endregion

        #region Properties 
        #endregion

        #region Constructors
        public LocationsStateHarness(LocationsState state)
            : base(state ?? new LocationsState())
        { }
        #endregion

        #region API Methods

        #region Add

        // public virtual async Task AddLocation(AmblOnGraph amblGraph, string username, string entLookup, UserLocation location)
        // {
        //     ensureStateObject();

        //     if (State.UserLayers.Any(x => x.ID == location.LayerID && !x.Shared))
        //     {
        //         var locationResp = await amblGraph.AddLocation(username, entLookup, location);

        //         if (locationResp.Status)
        //         {
        //             location.ID = locationResp.Model;

        //             if (State.SelectedUserLayerIDs.Contains(location.LayerID))
        //             {
        //                 State.VisibleUserLocations.Add(location);
        //                 State.AllUserLocations.Add(location);

        //                 var userMap = State.UserMaps.FirstOrDefault(x => x.ID == State.SelectedUserMapID);

        //                 if (userMap != null)
        //                     State.VisibleUserLocations = limitUserLocationsGeographically(State.VisibleUserLocations, userMap.Coordinates);

        //                 State.VisibleUserLocations = State.VisibleUserLocations.Distinct().ToList();
        //             }
        //         }
        //     }

        //     State.Loading = false;
        // }
        #endregion

        #region Delete

        // public virtual async Task DeleteLocation(AmblOnGraph amblGraph, string username, string entLookup, Guid locationID)
        // {
        //     ensureStateObject();

        //     var locationResp = await amblGraph.DeleteLocation(username, entLookup, locationID);

        //     if (locationResp.Status)
        //     {
        //         var existingVisible = State.VisibleUserLocations.FirstOrDefault(x => x.ID == locationID);

        //         if (existingVisible != null)
        //         {
        //             State.VisibleUserLocations.Remove(existingVisible);
        //             State.AllUserLocations.RemoveAll(item => item.ID == existingVisible.ID);
        //         }

        //         State.VisibleUserLocations = State.VisibleUserLocations.Distinct().ToList();
        //     }

        //     State.Loading = false;
        // }

        // public virtual async Task DedupLocationsByMap(AmblOnGraph amblGraph, string username, string entLookup, Guid mapID)
        // {
        //     ensureStateObject();

        //     var locationResp = await amblGraph.DedupLocationsByMap(username, entLookup, mapID);

        //     // Do not refresh state for now


        //     State.Loading = false;
        // }
        #endregion

        #region Edit

        // public virtual async Task EditLocation(AmblOnGraph amblGraph, string username, string entLookup, UserLocation location)
        // {
        //     ensureStateObject();

        //     if (State.UserLayers.Any(x => x.ID == location.LayerID && !x.Shared))
        //     {
        //         var locationResp = await amblGraph.EditLocation(username, entLookup, location);

        //         if (locationResp.Status)
        //         {
        //             if (State.SelectedUserLayerIDs.Contains(location.LayerID))
        //             {
        //                 var existingVisible = State.VisibleUserLocations.FirstOrDefault(x => x.ID == location.ID);

        //                 if (existingVisible != null)
        //                 {
        //                     State.VisibleUserLocations.Remove(existingVisible);
        //                     State.AllUserLocations.RemoveAll(item => item.ID == existingVisible.ID);
        //                 }

        //                 State.VisibleUserLocations.Add(location);
        //                 State.AllUserLocations.Add(location);

        //                 var userMap = State.UserMaps.FirstOrDefault(x => x.ID == State.SelectedUserMapID);

        //                 if (userMap != null)
        //                     State.VisibleUserLocations = limitUserLocationsGeographically(State.VisibleUserLocations, userMap.Coordinates);

        //                 State.VisibleUserLocations = State.VisibleUserLocations.Distinct().ToList();
        //             }
        //         }
        //     }

        //     State.Loading = false;
        // }
        
        #endregion

        public virtual async Task RefreshLocations(AmblOnGraph amblGraph, string entLookup, string username)
        {
            ensureStateObject();

            var userInfoResp = await amblGraph.GetUserInfo(username, entLookup);

            if (userInfoResp.Status)
            {
                State.UserInfo = userInfoResp.Model;
                State.UserInfo.Email = username;
            }

            State.AllUserLocations = await amblGraph.PopulateAllLocations(username, entLookup);

            State.Loading = false;
        }

        // public virtual async Task LoadCuratedLocationsIntoDB(AmblOnGraph amblGraph, string ownerUsername, string entLookup, List<dynamic> list, List<string> acclist, Guid layerID)
        // {
        //     float testFloat = 0;

        //     var workingList = list.Where(x => x.Latitude != null && float.TryParse(x.Latitude.ToString(), out testFloat)
        //         && x.Longitude != null && float.TryParse(x.Longitude.ToString(), out testFloat)).ToList();

        //     // Create location object
        //     await workingList.Each(async (jsonLocation) =>
        //     {
        //         var location = new UserLocation()
        //         {
        //             Address = jsonLocation.Address
        //             Country = jsonLocation.Country,
        //             Icon = jsonLocation.Icon,
        //             Instagram = jsonLocation.Instagram,
        //             Latitude = jsonLocation.Latitude,
        //             LayerID = layerID,
        //             Longitude = jsonLocation.Longitude,
        //             State = jsonLocation.State,
        //             Telephone = jsonLocation.Telephone,
        //             Title = jsonLocation.Title,
        //             Town = jsonLocation.Town,
        //             Website = jsonLocation.Website,
        //             ZipCode = jsonLocation.Zipcode
        //         };

        //         // Extract all properties of jsonLocation
        //         JObject propetiesObj = jsonLocation;

        //         var jsonProperties = propetiesObj.ToObject<Dictionary<string, object>>();

        //         // Create location object if it doesn't already exist in the graph DB
        //         var resp = await amblGraph.AddLocation(ownerUsername, entLookup, location);

        //         if (resp.Model != null)
        //         {
        //             // Iterate through accolade list 
        //             await acclist.Each(async (accName) =>
        //             {
        //                 // If it's in the JSON properties list for this location
        //                 var accKey = jsonProperties.Keys.FirstOrDefault(x => x == accName);

        //                 if (!String.IsNullOrEmpty(accKey) && (!String.IsNullOrEmpty(jsonProperties[accKey].ToString())))
        //                 {
        //                     UserAccolade accolade;

        //                 // Awkward logic to include support for Michelin stars
        //                 if (accKey == "Michelin")
        //                     {
        //                         accolade = new UserAccolade()
        //                         {
        //                             Rank = jsonProperties[accKey].ToString(),
        //                             Title = accKey,
        //                             Year = jsonProperties["Mich Since"].ToString()
        //                         };
        //                     }
        //                     else
        //                     {
        //                         accolade = new UserAccolade()
        //                         {
        //                             Rank = jsonProperties[accKey].ToString(),
        //                             Title = accKey
        //                         };
        //                     }
        //                     var accResp = await amblGraph.AddAccolade(ownerUsername, entLookup, accolade, resp.Model);
        //                 }
        //             });
        //         }
        //     });
        // }

        #endregion

        #region Helpers

        // Returns the radius and center of a circle inscribed within the bounded box

        protected virtual void ensureStateObject()
        {
            State.Error = "";

            //State.Status = "";

            if (State.SelectedUserLayerIDs == null)
                State.SelectedUserLayerIDs = new List<Guid>();

            if (State.VisibleUserLocations == null)
                State.VisibleUserLocations = new List<UserLocation>();

            if (State.LocalSearchUserLocations == null)
                State.LocalSearchUserLocations = new List<UserLocation>();

            if (State.OtherSearchUserLocations == null)
                State.OtherSearchUserLocations = new List<UserLocation>();

            State.AllUserLocations = State.AllUserLocations ?? new List<Location>();
        }

        // protected virtual async Task<List<UserAccolade>> fetchUserAccolades(AmblOnGraph amblGraph, string username, string entLookup, Guid locationId)
        // {
        //     var userAccolades = new List<UserAccolade>();

        //     var accolades = await amblGraph.ListAccolades(username, entLookup, locationId);

        //     accolades.Each((accolade) =>
        //     {
        //         userAccolades.Add(mapUserAccolade(accolade, locationId));
        //     });

        //     return userAccolades;
        // }

        #endregion

    }
}
