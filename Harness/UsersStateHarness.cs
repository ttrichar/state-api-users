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
            locations.Where(x => x.ID == Guid.Empty).ToList().ForEach(
                async (location) => {
                    var addLocationStatus = await amblGraph.AddLocation(location, details.Username);

                    if (addLocationStatus.Status)
                        location.ID = addLocationStatus.Model;
                });

            var createdMapResult = await amblGraph.AddMap(details.Username, map, locations.Select(loc => loc.ID).ToList());

            if (createdMapResult.Status)
            {
                await ListMaps();

                await SetPrimaryMap(createdMapResult.Model);

                state.PrimaryMapID = createdMapResult.Model;

                state.AddMapError = "";

                state.SelectedMapIDs.ForEach(
                    async (mapId) =>
                    {
                        await this.RemoveSelectedMapLocations(mapId);
                    });
                
                state.SelectedMapIDs = new List<Guid>();

                await AddSelectedMap(createdMapResult.Model);
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
            if (state.SelectedMapIDs == null)
                state.SelectedMapIDs = new List<Guid>();

            await WhenAll(
                ListMaps()
            );

            if (state.UserMaps.IsNullOrEmpty())
                await AddCuratedMap();

            var primary = state.UserMaps.FirstOrDefault(x => x.Primary == true);

            if (primary != null)
                state.PrimaryMapID = primary.ID;
            else if (state.UserMaps.Count > 0)
            {
                state.PrimaryMapID = state.UserMaps.First().ID;

                await amblGraph.SetPrimaryMap(details.Username, state.PrimaryMapID);
            }

            if (state.SelectedMapIDs.Count == 0 && state.PrimaryMapID != Guid.Empty)
            {
                state.SelectedMapIDs = new List<Guid>();

                await this.AddSelectedMap(state.PrimaryMapID);
            }

            return state;
        }

        public virtual async Task<UsersState> ListMaps()
        {
            log.LogInformation($"Listing maps for {details.Username}");
            
            state.UserMaps = await amblGraph.ListMaps(details.Username);

            state.UserMaps.ForEach(
                async (map) =>
                {
                    map.Locations = await amblGraph.ListMapLocations(details.Username, map.ID);
                });

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

        public virtual async Task<UsersState> AddSelectedMap(Guid mapId)
        {
            if (!mapId.IsEmpty() && state.UserMaps.Any(um => um.ID == mapId))
            {
                if (state.SelectedMapIDs.IndexOf(mapId) == -1)
                    state.SelectedMapIDs.Add(mapId);

                var mapLocations = await amblGraph.ListMapLocations(details.Username, mapId);

                mapLocations.ForEach(
                    (location) =>
                    {
                        if (!state.SelectedMapLocations.Any(x => x.ID == location.ID))
                        {
                            state.SelectedMapLocations.Add(new SelectedLocation()
                                {
                                    ID = location.ID,
                                    Address = location.Address,
                                    Country = location.Country,
                                    Icon = location.Icon,
                                    Instagram = location.Instagram,
                                    Latitude = location.Latitude,
                                    Longitude = location.Longitude,
                                    State = location.State,
                                    Telephone = location.Telephone,
                                    Title = location.Title,
                                    Town = location.Town,
                                    Website = location.Website,
                                    ZipCode = location.ZipCode,
                                    MapID = mapId
                                });
                        }
                    });
            }

            return state;
        }

        public virtual async Task<UsersState> RemoveSelectedMap(Guid mapId)
        {
            if (!mapId.IsEmpty() && state.UserMaps.Any(um => um.ID == mapId))
            {
                state.SelectedMapIDs = state.SelectedMapIDs.Where(x => x != mapId).ToList();
            }

            await RemoveSelectedMapLocations(mapId);

            return state;
        }

        public virtual async Task<UsersState> RemoveSelectedMapLocations(Guid mapId)
        {
            if (!mapId.IsEmpty() && state.UserMaps.Any(um => um.ID == mapId))
            {
                state.SelectedMapLocations = state.SelectedMapLocations.Where(x => x.MapID != mapId).ToList();
            }

            return state;
        }

        public virtual async Task<UsersState> SetPrimaryMap(Guid mapId)
        {
            log.LogInformation($"Setting primary map to {mapId.ToString()} for {details.Username}");
            
            var status = await amblGraph.SetPrimaryMap(details.Username, mapId);

            if (status)
                state.PrimaryMapID = mapId;

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