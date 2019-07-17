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

                return await SetSelectedMap(createdMapResult.Model);
            }

            //  TODO:  Add error to User Interface via State.AddMapError = createdMapResult.Status.Message

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

            return state;
        }

        public virtual async Task<UsersState> ListMaps()
        {
            log.LogInformation($"Listing maps for {details.Username}");
            
            state.UserMaps = await amblGraph.ListMaps(details.Username);

            return state;
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