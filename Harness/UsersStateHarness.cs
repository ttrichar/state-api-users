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
        public virtual async Task<UsersState> AddLocation(UserLocation location)
        {
            ensureStateObject();

            if (state.UserLayers.Any(x => x.ID == location.LayerID && !x.Shared))
            {
                var locationResp = await amblGraph.AddLocation(details.Username, details.EnterpriseAPIKey, location);

                if (locationResp.Status)
                {
                    location.ID = locationResp.Model;

                    if (state.SelectedUserLayerIDs.Contains(location.LayerID))
                    {
                        state.VisibleUserLocations.Add(location);

                        var userMap = state.UserMaps.FirstOrDefault(x => x.ID == state.SelectedUserMapID);

                        if (userMap != null)
                            state.VisibleUserLocations = limitUserLocationsGeographically(state.VisibleUserLocations, userMap.Coordinates);

                        state.VisibleUserLocations = state.VisibleUserLocations.Distinct().ToList();
                    }
                }
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> AddMap(UserMap map)
        {
            ensureStateObject();

            var mapResp = await amblGraph.AddMap(details.Username, details.EnterpriseAPIKey, map);

            if (mapResp.Status)
            {
                map.ID = mapResp.Model;

                state.UserMaps.Add(map);

                state.UserMaps = state.UserMaps.Distinct().ToList();

                state.SelectedUserMapID = map.ID;
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> AddSelectedLayer(Guid layerID)
        {
            ensureStateObject();

            if (state.UserLayers.Any(x => x.ID == layerID))
                state.SelectedUserLayerIDs.Add(layerID);

            var locationsToAdd = await fetchVisibleUserLocations(details.Username, details.EnterpriseAPIKey, new List<Guid>() { layerID });

            var userMap = state.UserMaps.FirstOrDefault(x => x.ID == state.SelectedUserMapID);

            if (userMap != null)
            {
                state.VisibleUserLocations.AddRange(limitUserLocationsGeographically(locationsToAdd, userMap.Coordinates));

                state.VisibleUserLocations = state.VisibleUserLocations.Distinct().ToList();
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> ChangeViewingArea(float[] coordinates)
        {
            ensureStateObject();

            var userMap = state.UserMaps.FirstOrDefault(x => x.ID == state.SelectedUserMapID);

            if (userMap != null)
            {
                userMap.Coordinates = coordinates;

                var visibleLocations = await fetchVisibleUserLocations(details.Username, details.EnterpriseAPIKey, state.SelectedUserLayerIDs);
                    
                state.VisibleUserLocations = limitUserLocationsGeographically(visibleLocations, userMap.Coordinates);

                state.VisibleUserLocations = state.VisibleUserLocations.Distinct().ToList();
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> DeleteLocation(Guid locationID)
        {
            ensureStateObject();

            var locationResp = await amblGraph.DeleteLocation(details.Username, details.EnterpriseAPIKey, locationID);

            if (locationResp.Status)
            {
                var existingVisible = state.VisibleUserLocations.FirstOrDefault(x => x.ID == locationID);

                if (existingVisible != null)
                    state.VisibleUserLocations.Remove(existingVisible);

                state.VisibleUserLocations = state.VisibleUserLocations.Distinct().ToList();
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> DeleteMap(Guid mapID)
        {
            ensureStateObject();

            var userMap = state.UserMaps.FirstOrDefault(x => x.ID == mapID);

            if (userMap != null && !userMap.Shared && userMap.Deletable)
            {
                var mapResp = await amblGraph.DeleteMap(details.Username, details.EnterpriseAPIKey, mapID);

                if (mapResp.Status)
                {
                    var existingMap = state.UserMaps.FirstOrDefault(x => x.ID == mapID);

                    if (existingMap != null)
                        state.UserMaps.Remove(existingMap);
                    
                    state.UserMaps = state.UserMaps.Distinct().ToList();

                    if (!state.UserMaps.Any(x => x.Primary == true))
                    {
                        var newPrimary = state.UserMaps.FirstOrDefault(x => x.Shared && !x.Deletable);

                        if (newPrimary != null)
                            newPrimary.Primary = true;
                        else if (state.UserMaps.Count > 0)
                            state.UserMaps.First().Primary = true;
                    }

                    if (state.UserMaps.Any(x => x.Primary))
                    {
                        var primaryMap = state.UserMaps.First(x => x.Primary);

                        state.SelectedUserMapID = primaryMap.ID;
                    
                        state.SelectedUserLayerIDs.Clear();

                        state.SelectedUserLayerIDs.Add(primaryMap.DefaultLayerID);

                        var visibleLocations = await fetchVisibleUserLocations(details.Username, details.EnterpriseAPIKey, state.SelectedUserLayerIDs);
                    
                        state.VisibleUserLocations = limitUserLocationsGeographically(visibleLocations, primaryMap.Coordinates);

                        state.VisibleUserLocations = state.VisibleUserLocations.Distinct().ToList();
                    }
                }
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> EditLocation(UserLocation location)
        {
            ensureStateObject();

            if (state.UserLayers.Any(x => x.ID == location.LayerID && !x.Shared))
            {
                var locationResp = await amblGraph.EditLocation(details.Username, details.EnterpriseAPIKey, location);

                if (locationResp.Status)
                {
                    if (state.SelectedUserLayerIDs.Contains(location.LayerID))
                    {
                        var existingVisible = state.VisibleUserLocations.FirstOrDefault(x => x.ID == location.ID);

                        if (existingVisible != null)
                            state.VisibleUserLocations.Remove(existingVisible);

                        state.VisibleUserLocations.Add(location);

                        var userMap = state.UserMaps.FirstOrDefault(x => x.ID == state.SelectedUserMapID);

                        if (userMap != null)
                            state.VisibleUserLocations = limitUserLocationsGeographically(state.VisibleUserLocations, userMap.Coordinates);

                        state.VisibleUserLocations = state.VisibleUserLocations.Distinct().ToList();
                    }
                }
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> EditMap(UserMap map)
        {
            ensureStateObject();

            var userMap = state.UserMaps.FirstOrDefault(x => x.ID == map.ID);

            if (userMap != null && !userMap.Shared)
            {
                var mapResp = await amblGraph.EditMap(details.Username, details.EnterpriseAPIKey, map);

                if (mapResp.Status)
                {
                    var existingMap = state.UserMaps.FirstOrDefault(x => x.ID == map.ID);

                    if (existingMap != null)
                    {
                        state.UserMaps.Remove(existingMap);

                        state.UserMaps.Add(map);

                        state.UserMaps = state.UserMaps.Distinct().ToList();
                    }
                }
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> Ensure()
        {
            ensureStateObject();

            state.UserLayers = await fetchUserLayers(details.Username, details.EnterpriseAPIKey);

            state.UserMaps = await fetchUserMaps(details.Username, details.EnterpriseAPIKey);

            if (state.SelectedUserMapID.IsEmpty())
            {
                var primaryMap = state.UserMaps.FirstOrDefault(x => x.Primary == true);

                if (primaryMap != null)
                    state.SelectedUserMapID = primaryMap.ID;
            }

            var visibleLocations = await fetchVisibleUserLocations(details.Username, details.EnterpriseAPIKey, state.SelectedUserLayerIDs);

            var userMap = state.UserMaps.FirstOrDefault(x => x.ID == state.SelectedUserMapID);
            
            if (userMap != null)
            {
                state.VisibleUserLocations = limitUserLocationsGeographically(visibleLocations, userMap.Coordinates);

                state.VisibleUserLocations = state.VisibleUserLocations.Distinct().ToList();
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> Load()
        {
            ensureStateObject();

            state.UserLayers = await fetchUserLayers(details.Username, details.EnterpriseAPIKey);

            state.UserMaps = await fetchUserMaps(details.Username, details.EnterpriseAPIKey);

            var primaryMap = state.UserMaps.FirstOrDefault(x => x.Primary == true);

            if (primaryMap != null)
            {
                state.SelectedUserMapID = primaryMap.ID;

                var userMap = state.UserMaps.FirstOrDefault(x => x.ID == state.SelectedUserMapID);

                if (userMap != null)
                {
                    state.SelectedUserLayerIDs.Clear();

                    state.SelectedUserLayerIDs.Add(userMap.DefaultLayerID);

                    var visibleLocations = await fetchVisibleUserLocations(details.Username, details.EnterpriseAPIKey, state.SelectedUserLayerIDs);
                    
                    state.VisibleUserLocations = limitUserLocationsGeographically(visibleLocations, userMap.Coordinates);

                    state.VisibleUserLocations = state.VisibleUserLocations.Distinct().ToList();
                }
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> RemoveSelectedLayer(Guid layerID)
        {
            ensureStateObject();

            state.SelectedUserLayerIDs.Remove(layerID);

            state.VisibleUserLocations = removeUserLocationsByLayerID(state.VisibleUserLocations, layerID);

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> SetSelectedMap(Guid mapID)
        {
            ensureStateObject();

            var userMap = state.UserMaps.FirstOrDefault(x => x.ID == mapID);

            if (userMap != null)
            {
                state.SelectedUserMapID = mapID;

                var visibleLocations = await fetchVisibleUserLocations(details.Username, details.EnterpriseAPIKey, state.SelectedUserLayerIDs);
                    
                state.VisibleUserLocations = limitUserLocationsGeographically(visibleLocations, userMap.Coordinates);

                state.VisibleUserLocations = state.VisibleUserLocations.Distinct().ToList();
            }
            
            state.Loading = false;

            return state;
        }
        #endregion

        #region Helpers
        protected virtual void ensureStateObject()
        {
            if (state.SelectedUserLayerIDs == null)
                state.SelectedUserLayerIDs = new List<Guid>();
            
            if (state.UserLayers == null)
                state.UserLayers = new List<UserLayer>();

            if (state.UserMaps == null)
                state.UserMaps = new List<UserMap>();

            if (state.VisibleUserLocations == null)
                state.VisibleUserLocations = new List<UserLocation>();
        }

        protected virtual async Task<List<UserLayer>> fetchUserLayers(string email, string entAPIKey)
        {
            var userLayers = new List<UserLayer>();

            var layers = await amblGraph.ListLayers(email, entAPIKey);

            layers.ForEach(
                (layer) =>
                {
                    userLayers.Add(mapUserLayer(layer));
                });

            var sharedLayers = await amblGraph.ListSharedLayers(email, entAPIKey);

            sharedLayers.ForEach(
                (layerInfo) =>
                {
                    float[] coords = null;

                    var associatedMap = state.UserMaps.FirstOrDefault(x => x.ID == layerInfo.Item1.DefaultMapID);

                    if (associatedMap != null)
                        coords = associatedMap.Coordinates;

                    userLayers.Add(mapUserLayer(layerInfo.Item1, layerInfo.Item2, coords));
                });

            return userLayers;
        }

        protected virtual async Task<List<UserMap>> fetchUserMaps(string email, string entAPIKey)
        {
            var userMaps = new List<UserMap>();

            var maps = await amblGraph.ListMaps(email, entAPIKey);

            maps.ForEach(
                (map) =>
                {
                    userMaps.Add(mapUserMap(map));
                });

            var sharedMaps = await amblGraph.ListSharedMaps(email, entAPIKey);

            sharedMaps.ForEach(
                (mapInfo) =>
                {
                    userMaps.Add(mapUserMap(mapInfo.Item1, mapInfo.Item2));
                });

            return userMaps;
        }

        protected virtual async Task<List<UserLocation>> fetchVisibleUserLocations(string email, string entAPIKey,List<Guid> layerIDs)
        {
            var userLocations = new List<UserLocation>();

            layerIDs.ForEach(
                (layerID) =>
                {
                    var layer = state.UserLayers.FirstOrDefault(x => x.ID == layerID);

                    var locations = amblGraph.ListLocations(email, entAPIKey, layerID).GetAwaiter().GetResult();

                    locations.ForEach(
                        (location) =>
                        {
                            userLocations.Add(mapUserLocation(location, layerID, state.UserLayers.Any(x => x.ID == layerID && !x.Shared)));
                        });

                    if (layer != null && layer.Coordinates != null)
                        userLocations = limitUserLocationsGeographically(userLocations, layer.Coordinates);
                });

            return userLocations;
        }

        protected virtual List<UserLocation> limitUserLocationsGeographically(List<UserLocation> userLocations, float[] coordinates)
        {
            return userLocations.Where(x => x.Latitude >= coordinates[0]
                                    && x.Latitude <= coordinates[2]
                                    && x.Longitude >= coordinates[1]
                                    && x.Longitude <= coordinates[3]).ToList();
        }

        protected virtual UserLayer mapUserLayer(Layer layer)
        {
            return new UserLayer()
            {
                ID = layer.ID,
                Deletable = true,
                Shared = false,
                Title = layer.Title,
                InheritedID = layer.ID
            };
        }

        protected virtual UserLayer mapUserLayer(SharedLayer layer, Layer parent, float[] coordinates)
        {
            return new UserLayer()
            {
                ID = layer.ID,
                Coordinates = coordinates,
                Deletable = layer.Deletable,
                Shared = true,
                Title = layer.Title,
                InheritedID = parent.ID
            };
        }

        protected virtual UserLocation mapUserLocation(Location location, Guid layerID, bool userOwns)
        {
            return new UserLocation()
            {
                ID = location.ID,
                Address = location.Address,
                Country = location.Country,
                Deletable = userOwns,
                Icon = location.Icon,
                Instagram = location.Instagram,
                Latitude = location.Latitude,
                LayerID = layerID,
                Longitude = location.Longitude,
                State = location.State,
                Telephone = location.Telephone,
                Title = location.Title,
                Town = location.Town,
                Website = location.Website,
                ZipCode = location.ZipCode
            }
        }

        protected virtual UserMap mapUserMap(Map map)
        {
            return new UserMap()
            {
                ID = map.ID,
                Coordinates = map.Coordinates,
                DefaultLayerID = map.DefaultLayerID,
                Deletable = true,
                Latitude = map.Latitude,
                Longitude = map.Longitude,
                Primary = map.Primary,
                Shared = false,
                Title = map.Title,
                Zoom = map.Zoom,
                InheritedID = map.ID
            };
        }

        protected virtual UserMap mapUserMap(SharedMap map, Map parent)
        {
            return new UserMap()
            {
                ID = map.ID,
                Coordinates = parent.Coordinates,
                DefaultLayerID = parent.DefaultLayerID,
                Deletable = map.Deletable,
                Latitude = parent.Latitude,
                Longitude = parent.Longitude,
                Primary = false,
                Shared = true,
                Title = map.Title,
                Zoom = parent.Zoom
            };
        }

        protected virtual List<UserLocation> removeUserLocationsByLayerID(List<UserLocation> userLocations, Guid layerID)
        {
            return userLocations.Where(x => x.LayerID != layerID).ToList();
        }
        #endregion
    }
}