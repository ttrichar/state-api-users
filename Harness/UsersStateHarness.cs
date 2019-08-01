using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AmblOn.State.API.Users.Graphs;
using AmblOn.State.API.Users.Models;
using Fathym;
using Fathym.Design.Singleton;
using LCU.Graphs.Registry.Enterprises;
using LCU.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace AmblOn.State.API.Users.Harness
{
    public class UsersStateHarness : LCUStateHarness<UsersState>
    {
        #region Fields
        protected readonly AmblOnGraph amblGraph;
        #endregion

        #region Properties
        #endregion

        #region Constructors
        public UsersStateHarness(HttpRequest req, ILogger log, UsersState state)
            : base(req, log, state)
        {
            amblGraph = req.LoadGraph<AmblOnGraph>();
        }
        #endregion

        #region API Methods
        public virtual async Task<UsersState> AddMap(Map map, List<Location> locations)
        {
            var createdMapResult = await amblGraph.AddMap(details.Username, map, locations.Select(loc => loc.ID).ToList());

            if (createdMapResult.Status)
            {
                await ListMaps();

                state.AddMapError = "";

                return await SetSelectedMap(createdMapResult.Model);
            }
            else
                state.AddMapError = createdMapResult.Status.Message;
            
            return state;
        }

        public virtual async Task<UsersState> AddCuratedMap()
        {
            var defaultLocations = await amblGraph.ListDefaultLocations();

            var layer = createDefaultLayer();

            return await AddMap(layer, defaultLocations);
        }

        public virtual async Task<UsersState> Ensure()
        {
            await WhenAll(
                ListMaps()
            );

            if (state.UserMaps.IsNullOrEmpty())
                await AddCuratedMap();

            if (state.SelectedMapID.IsEmpty() || !state.UserMaps.Any(x => x.ID == state.SelectedMapID))
                state.SelectedMapID = state.UserMaps.First().ID;

            state.SelectedMapLocations = await amblGraph.ListMapLocations(details.Username, state.SelectedMapID);

            return state;
        }

        public virtual async Task<UsersState> ListMaps()
        {
            log.LogInformation($"Listing maps for {details.Username}");
            
            state.UserMaps = await amblGraph.ListMaps(details.Username);

            return state;
        }

        public virtual async Task LoadCuratedLocationsIntoDB(string json)
        {
            var list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(json);
            float testFloat = 0;

            list.Where(x => x.Latitude != null && float.TryParse(x.Latitude.ToString(), out testFloat)
                && x.Longitude != null && float.TryParse(x.Longitude.ToString(), out testFloat)).ToList()
                .ForEach(
               async (jsonLocation) =>
                {
                    var location = new Location()
                    {
                        Address = jsonLocation.Address,
                        Country = jsonLocation.Country,
                        Icon = jsonLocation.Icon,
                        Instagram = jsonLocation.Instagram,
                        Latitude = jsonLocation.Latitude,
                        Longitude = jsonLocation.Longitude,
                        State = jsonLocation.State,
                        Telephone = jsonLocation.Telephone,
                        Title = jsonLocation.Title,
                        Town = jsonLocation.Town,
                        Website = jsonLocation.Website,
                        ZipCode = jsonLocation.Zipcode
                    };

                    var resp = await amblGraph.AddLocation(location, "0eb58bc9-01cc-4238-b23b-9f564993a368");
                });
        }

        public virtual async Task<UsersState> SetSelectedMap(Guid mapId)
        {
            if (!mapId.IsEmpty() && state.UserMaps.Any(um => um.ID == mapId))
            {
                state.SelectedMapID = mapId;

                state.SelectedMapLocations = await amblGraph.ListMapLocations(details.Username, state.SelectedMapID);
            }

            return state;
        }
        #endregion

        #region Helpers
        protected virtual Map createDefaultLayer()
        {
            return new Map()
            {
                Lookup = "TheSpecialCuratedMap",
                Title = "Default Map",
                Latitude = 40,
                Longitude = -105,
                Zoom = 5
            };
        }
        #endregion
    }
}