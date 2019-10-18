using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AmblOn.State.API.Users.Graphs;
using AmblOn.State.API.Users.Models;
using Fathym;
using Fathym.API;
using Fathym.Design.Singleton;
using LCU.Graphs;
using LCU.Graphs.Registry.Enterprises;
using LCU.StateAPI;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Device.Location;
using LCU.Presentation;
using Microsoft.AspNetCore.WebUtilities;
using LCU.Personas.Client.Applications;
using Microsoft.AspNetCore.Http.Internal;
using System.IO;
using LCU.Personas.Client.Enterprises;

namespace AmblOn.State.API.Users.Harness
{
    public class UsersStateHarness : LCUStateHarness<UsersState>
    {
        #region Fields
        protected readonly AmblOnGraph amblGraph;

        protected readonly ApplicationManagerClient appMgr;

        protected readonly Guid enterpriseId;

        protected readonly EnterpriseManagerClient entMgr;

        #endregion

        #region Properties
        #endregion

        #region Constructors
        public UsersStateHarness(HttpRequest req, ILogger log, UsersState state)
            : base(req, log, state)
        {
            amblGraph = new AmblOnGraph(new LCUGraphConfig()
            {
                APIKey = Environment.GetEnvironmentVariable("LCU-GRAPH-API-KEY"),
                Database = Environment.GetEnvironmentVariable("LCU-GRAPH-DATABASE"),
                Graph = Environment.GetEnvironmentVariable("LCU-GRAPH"),
                Host = Environment.GetEnvironmentVariable("LCU-GRAPH-HOST")
            });

            appMgr = req.ResolveClient<ApplicationManagerClient>(logger);

            entMgr = req.ResolveClient<EnterpriseManagerClient>(logger);

            var enterprise = entMgr.GetEnterprise(details.EnterpriseAPIKey).GetAwaiter().GetResult();

            enterpriseId = enterprise.Model.ID;
        }
        #endregion

        #region API Methods
        #region Add
        public virtual async Task<UsersState> AddAccolade(UserAccolade accolade, Guid layerId)
        {
            ensureStateObject();

            var accoladeResp = await amblGraph.AddAccolade(details.Username, details.EnterpriseAPIKey, accolade, layerId);

            if (accoladeResp.Status)
            {
                accolade.ID = accoladeResp.Model;

                if (!state.UserAccolades.Any(x => x.ID == accolade.ID))
                    state.UserAccolades.Add(accolade);
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> AddAlbum(UserAlbum album, List<ImageMessage> images)
        {
            ensureStateObject();

            album.Photos = mapImageDataToUserPhotos(album.Photos, images);

            var albumResp = await amblGraph.AddAlbum(details.Username, details.EnterpriseAPIKey, album);

            if (albumResp.Status)
            {
                album.ID = albumResp.Model;

                if (!state.UserAlbums.Any(x => x.ID == album.ID))
                    state.UserAlbums.Add(album);

                if (album.Photos.Count > 0)
                {
                    album.Photos.ForEach(
                        (photo) =>
                        {
                            AddPhoto(photo, album.ID.HasValue ? album.ID.Value : Guid.Empty, photo.LocationID.HasValue ? photo.LocationID.Value : Guid.Empty).GetAwaiter().GetResult();
                        });
                }
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> AddItinerary(UserItinerary itinerary)
        {
            ensureStateObject();

            itinerary.CreatedDateTime = DateTime.Now;
            var itineraryResp = await amblGraph.AddItinerary(details.Username, details.EnterpriseAPIKey, itinerary);

            if (itineraryResp.Status)
            {
                itinerary.ID = itineraryResp.Model;
                itinerary.Activities = new List<UserItineraryActivity>();

                if (!state.UserItineraries.Any(x => x.ID == itinerary.ID))
                    state.UserItineraries.Add(itinerary);
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> AddItineraryActivity(UserItineraryActivity itineraryActivity, Guid itineraryID, Guid locationID)
        {
            ensureStateObject();

            itineraryActivity.CreatedDateTime = DateTime.Now;
            var itineraryActivityResp = await amblGraph.AddItineraryActivity(details.Username, details.EnterpriseAPIKey, itineraryActivity, itineraryID, locationID);

            if (itineraryActivityResp.Status)
            {
                itineraryActivity.ID = itineraryActivityResp.Model;

                var itinerary = state.UserItineraries.FirstOrDefault(x => x.ID == itineraryID);

                if (itinerary != null)
                {
                    itinerary.Activities.Add(itineraryActivity);

                    itinerary.Activities = itinerary.Activities.Distinct().ToList();
                };
            }

            state.Loading = false;

            return state;
        }

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

            BaseResponse<Guid> mapResp = new BaseResponse<Guid>() { Status = Status.Initialized };

            if (!map.Shared)
                mapResp = await amblGraph.AddMap(details.Username, details.EnterpriseAPIKey, map);
            else
                mapResp = await amblGraph.AddSharedMap(details.Username, details.EnterpriseAPIKey, map, (map.InheritedID.HasValue ? map.InheritedID.Value : Guid.Empty));

            if (mapResp.Status)
            {
                map.ID = mapResp.Model;

                state.UserMaps.Add(map);

                state.UserMaps = state.UserMaps.Distinct().ToList();

                state.SelectedUserMapID = map.ID.Value;
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> AddPhoto(UserPhoto photo, Guid albumID, Guid locationID)
        {
            ensureStateObject();

            await appMgr.SaveFile(photo.ImageData.Data, enterpriseId, "admin/" + details.Username + "/albums/" + albumID.ToString(), QueryHelpers.ParseQuery(photo.ImageData.Headers)["filename"], new Guid(details.ApplicationID), "/");

            photo.URL = "/admin/" + details.Username + "/albums/" + albumID.ToString() + "/" + QueryHelpers.ParseQuery(photo.ImageData.Headers)["filename"];

            photo.ImageData = null;

            var photoResp = await amblGraph.AddPhoto(details.Username, details.EnterpriseAPIKey, photo, albumID, locationID);

            if (photoResp.Status)
            {
                photo.ID = photoResp.Model;
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

        public virtual async Task<UsersState> AddTopList(UserTopList topList)
        {
            ensureStateObject();

            var topListResp = await amblGraph.AddTopList(details.Username, details.EnterpriseAPIKey, topList);

            if (topListResp.Status)
            {
                topList.ID = topListResp.Model;

                if (!state.UserTopLists.Any(x => x.ID == topList.ID))
                    state.UserTopLists.Add(topList);
            }

            state.Loading = false;

            return state;
        }
        #endregion
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

        #region Delete
        public virtual async Task<UsersState> DeleteAccolades(Guid[] accoladeIDs, Guid layerId)
        {
            ensureStateObject();

            var accoladeResp = await amblGraph.DeleteAccolades(details.Username, details.EnterpriseAPIKey, accoladeIDs, layerId);

            if (accoladeResp.Status)
            {
                state.UserAccolades.RemoveAll(x => accoladeIDs.ToList<Guid>().Contains(x.ID ?? Guid.Empty));

                state.UserAccolades = state.UserAccolades.Distinct().ToList();
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> DeleteMaps(Guid[] mapIDs)
        {
            ensureStateObject();

            var mapResp = await amblGraph.DeleteMaps(details.Username, details.EnterpriseAPIKey, mapIDs);

            if (mapResp.Status)
            {
                state.UserMaps.RemoveAll(x => mapIDs.ToList().Contains(x.ID ?? default(Guid)));

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

                    state.SelectedUserMapID = (primaryMap.ID.HasValue ? primaryMap.ID.Value : Guid.Empty);

                    state.SelectedUserLayerIDs.Clear();

                    state.SelectedUserLayerIDs.Add(primaryMap.DefaultLayerID);

                    var visibleLocations = await fetchVisibleUserLocations(details.Username, details.EnterpriseAPIKey, state.SelectedUserLayerIDs);

                    state.VisibleUserLocations = limitUserLocationsGeographically(visibleLocations, primaryMap.Coordinates);

                    state.VisibleUserLocations = state.VisibleUserLocations.Distinct().ToList();
                }
            }


            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> DeleteAlbum(Guid albumID)
        {
            ensureStateObject();

            var albumResp = await amblGraph.DeleteAlbum(details.Username, details.EnterpriseAPIKey, albumID);

            if (albumResp.Status)
            {
                var existing = state.UserAlbums.FirstOrDefault(x => x.ID == albumID);

                if (existing != null)
                    state.UserAlbums.Remove(existing);

                state.UserAlbums = state.UserAlbums.Distinct().ToList();
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> DeleteItinerary(Guid itineraryID)
        {
            ensureStateObject();

            var itineraryResp = await amblGraph.DeleteItinerary(details.Username, details.EnterpriseAPIKey, itineraryID);

            if (itineraryResp.Status)
            {
                var existing = state.UserItineraries.FirstOrDefault(x => x.ID == itineraryID);

                if (existing != null)
                    state.UserItineraries.Remove(existing);

                state.UserItineraries = state.UserItineraries.Distinct().ToList();
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> DeleteItineraryActivity(Guid itineraryActivityID)
        {
            ensureStateObject();

            var itineraryActivityResp = await amblGraph.DeleteItineraryActivity(details.Username, details.EnterpriseAPIKey, itineraryActivityID);

            if (itineraryActivityResp.Status)
            {
                var existingItinerary = state.UserItineraries.FirstOrDefault(x => x.Activities.Any(y => y.ID == itineraryActivityID));

                if (existingItinerary != null)
                {
                    var existingItineraryActivity = existingItinerary.Activities.FirstOrDefault(x => x.ID == itineraryActivityID);

                    if (existingItineraryActivity != null)
                    {
                        existingItinerary.Activities.Remove(existingItineraryActivity);

                        existingItinerary.Activities = existingItinerary.Activities.Distinct().ToList();
                    }
                }
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

            if (userMap != null && userMap.Deletable)
            {
                BaseResponse mapResp = new BaseResponse() { Status = Status.Initialized };

                if (!userMap.Shared)
                    mapResp = await amblGraph.DeleteMap(details.Username, details.EnterpriseAPIKey, mapID);
                else
                    mapResp = await amblGraph.DeleteSharedMap(details.Username, details.EnterpriseAPIKey, mapID);

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

                        state.SelectedUserMapID = (primaryMap.ID.HasValue ? primaryMap.ID.Value : Guid.Empty);

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

        public virtual async Task<UsersState> DeletePhoto(Guid photoID)
        {
            ensureStateObject();

            var photoResp = await amblGraph.DeletePhoto(details.Username, details.EnterpriseAPIKey, photoID);

            if (photoResp.Status)
            {
                var existingAlbum = state.UserAlbums.FirstOrDefault(x => x.Photos.Any(y => y.ID == photoID));

                if (existingAlbum != null)
                {
                    var existingPhoto = existingAlbum.Photos.FirstOrDefault(x => x.ID == photoID);

                    if (existingPhoto != null)
                    {
                        existingAlbum.Photos.Remove(existingPhoto);

                        existingAlbum.Photos = existingAlbum.Photos.Distinct().ToList();
                    }
                }
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> DeleteTopList(Guid topListID)
        {
            ensureStateObject();

            var topListResp = await amblGraph.DeleteTopList(details.Username, details.EnterpriseAPIKey, topListID);

            if (topListResp.Status)
            {
                var existing = state.UserTopLists.FirstOrDefault(x => x.ID == topListID);

                if (existing != null)
                    state.UserTopLists.Remove(existing);

                state.UserTopLists = state.UserTopLists.Distinct().ToList();
            }

            state.Loading = false;

            return state;
        }

        #endregion

        #region Edit
        public virtual async Task<UsersState> EditAccolade(UserAccolade accolade, Guid layerId)
        {
            ensureStateObject();

            var existing = state.UserAccolades.FirstOrDefault(x => x.ID == accolade.ID);

            if (existing != null)
            {
                var accoladeResp = await amblGraph.EditAccolade(details.Username, details.EnterpriseAPIKey, accolade, layerId);

                if (accoladeResp.Status)
                {

                    state.UserAccolades.Remove(existing);

                    state.UserAccolades.Add(accolade);

                    state.UserAccolades = state.UserAccolades.Distinct().ToList();
                }
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> EditAlbum(UserAlbum album)
        {
            ensureStateObject();

            var existing = state.UserAlbums.FirstOrDefault(x => x.ID == album.ID);

            if (existing != null)
            {
                var albumResp = await amblGraph.EditAlbum(details.Username, details.EnterpriseAPIKey, album);

                if (albumResp.Status)
                {
                    state.UserAlbums.Remove(existing);

                    state.UserAlbums.Add(album);

                    state.UserAlbums = state.UserAlbums.Distinct().ToList();
                }
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> EditItinerary(UserItinerary itinerary)
        {
            ensureStateObject();

            var existing = state.UserItineraries.FirstOrDefault(x => x.ID == itinerary.ID);

            if (existing != null)
            {
                var itineraryResp = await amblGraph.EditItinerary(details.Username, details.EnterpriseAPIKey, itinerary);

                if (itineraryResp.Status)
                {
                    state.UserItineraries.Remove(existing);

                    state.UserItineraries.Add(itinerary);

                    state.UserItineraries = state.UserItineraries.Distinct().ToList();
                }
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> EditItineraryActivity(UserItineraryActivity itineraryActivity, Guid itineraryID)
        {
            ensureStateObject();

            var existingItinerary = state.UserItineraries.FirstOrDefault(x => x.Activities.Any(y => y.ID == itineraryActivity.ID));

            if (existingItinerary != null)
            {
                var existingItineraryActivity = existingItinerary.Activities.FirstOrDefault(x => x.ID == itineraryActivity.ID);

                if (existingItineraryActivity != null)
                {
                    var itineraryActivityResp = await amblGraph.EditItineraryActivity(details.Username, details.EnterpriseAPIKey, itineraryActivity, itineraryID);

                    if (itineraryActivityResp.Status)
                    {
                        existingItinerary.Activities.Remove(existingItineraryActivity);

                        existingItinerary.Activities.Add(itineraryActivity);

                        existingItinerary.Activities = existingItinerary.Activities.Distinct().ToList();
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

            BaseResponse mapResp = new BaseResponse() { Status = Status.Initialized };

            if (userMap != null && !userMap.Shared)
                mapResp = await amblGraph.EditMap(details.Username, details.EnterpriseAPIKey, map);
            else if (userMap != null)
                mapResp = await amblGraph.EditSharedMap(details.Username, details.EnterpriseAPIKey, map);

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

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> EditPhoto(UserPhoto photo, Guid albumID)
        {
            ensureStateObject();

            var existingAlbum = state.UserAlbums.FirstOrDefault(x => x.Photos.Any(y => y.ID == photo.ID));

            if (existingAlbum != null)
            {
                var existingPhoto = existingAlbum.Photos.FirstOrDefault(x => x.ID == photo.ID);

                if (existingPhoto != null)
                {
                    //SEND NEW PHOTO BYTES
                    photo.URL = "https://static01.nyt.com/images/2019/08/21/movies/21xp-matrix/21xp-matrix-articleLarge.jpg?quality=90&auto=webp";

                    var photoResp = await amblGraph.EditPhoto(details.Username, details.EnterpriseAPIKey, photo, albumID);

                    if (photoResp.Status)
                    {
                        existingAlbum.Photos.Remove(existingPhoto);

                        existingAlbum.Photos.Add(photo);

                        existingAlbum.Photos = existingAlbum.Photos.Distinct().ToList();
                    }
                }
            }

            state.Loading = false;

            return state;
        }
        public virtual async Task<UsersState> EditTopList(UserTopList topList)
        {
            ensureStateObject();

            var existing = state.UserTopLists.FirstOrDefault(x => x.ID == topList.ID);

            if (existing != null)
            {
                var topListResp = await amblGraph.EditTopList(details.Username, details.EnterpriseAPIKey, topList);

                if (topListResp.Status)
                {

                    state.UserTopLists.Remove(existing);

                    state.UserTopLists.Add(topList);

                    state.UserTopLists = state.UserTopLists.Distinct().ToList();
                }
            }

            state.Loading = false;

            return state;
        }
        #endregion

        public virtual async Task<UsersState> Ensure()
        {
            ensureStateObject();

            state.UserAlbums = await fetchUserAlbums(details.Username, details.EnterpriseAPIKey);

            state.UserItineraries = await fetchUserItineraries(details.Username, details.EnterpriseAPIKey);

            state.UserLayers = await fetchUserLayers(details.Username, details.EnterpriseAPIKey);

            state.UserMaps = await fetchUserMaps(details.Username, details.EnterpriseAPIKey);

            if (state.SelectedUserMapID.IsEmpty())
            {
                var primaryMap = state.UserMaps.FirstOrDefault(x => x.Primary == true);

                if (primaryMap != null)
                    state.SelectedUserMapID = (primaryMap.ID.HasValue ? primaryMap.ID.Value : Guid.Empty);
            }

            var visibleLocations = await fetchVisibleUserLocations(details.Username, details.EnterpriseAPIKey, state.SelectedUserLayerIDs);

            var userMap = state.UserMaps.FirstOrDefault(x => x.ID == state.SelectedUserMapID);

            if (userMap != null)
            {
                state.VisibleUserLocations = limitUserLocationsGeographically(visibleLocations, userMap.Coordinates);

                state.VisibleUserLocations = state.VisibleUserLocations.Distinct().ToList();
            }

            var userLayer = state.UserLayers.Where(x => x.Title == "User").FirstOrDefault();

            var userLayerID = (userLayer == null) ? Guid.Empty : userLayer.ID;

            state.UserTopLists = await fetchUserTopLists(details.Username, details.EnterpriseAPIKey, userLayerID);

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> GlobalSearch(string searchTerm)
        {
            ensureStateObject();

            var userMap = state.UserMaps.FirstOrDefault(x => x.ID == state.SelectedUserMapID);

            if (userMap != null)
            {
                var radius = computeSearchRadius(userMap.Coordinates[0], userMap.Coordinates[1], userMap.Coordinates[2], userMap.Coordinates[3]);

                var allLocations = await fetchVisibleUserLocations(details.Username, details.EnterpriseAPIKey, state.UserLayers.Select(x => x.ID).ToList());

                var searchLocations = limitUserLocationsBySearch(allLocations, searchTerm);

                var centerCoords = computeCenter(userMap.Coordinates[0], userMap.Coordinates[1], userMap.Coordinates[2], userMap.Coordinates[3]);

                var radiusLocations = limitUserLocationsByRadius(searchLocations, radius, centerCoords.Item1, centerCoords.Item2);

                state.LocalSearchUserLocations = radiusLocations
                        .Distinct()
                        .OrderBy(x => x.Title)
                        .ToList();

                var localIDs = state.LocalSearchUserLocations.Select(x => x.ID);

                state.OtherSearchUserLocations = searchLocations
                        .Where(x => !localIDs.Contains(x.ID))
                        .Distinct()
                        .OrderBy(x => x.Title)
                        .ToList();
            }

            state.Loading = false;

            return state;
        }

        public virtual async Task<UsersState> Load()
        {
            ensureStateObject();

            state.UserAlbums = await fetchUserAlbums(details.Username, details.EnterpriseAPIKey);

            state.UserItineraries = await fetchUserItineraries(details.Username, details.EnterpriseAPIKey);

            state.UserLayers = await fetchUserLayers(details.Username, details.EnterpriseAPIKey);

            state.UserMaps = await fetchUserMaps(details.Username, details.EnterpriseAPIKey);

            var primaryMap = state.UserMaps.FirstOrDefault(x => x.Primary == true);

            if (primaryMap != null)
            {
                state.SelectedUserMapID = (primaryMap.ID.HasValue ? primaryMap.ID.Value : Guid.Empty);

                var userMap = state.UserMaps.FirstOrDefault(x => x.ID == state.SelectedUserMapID);

                if (userMap != null)
                {
                    state.SelectedUserLayerIDs.Clear();

                    var layerID = userMap.DefaultLayerID;

                    var layer = state.UserLayers.FirstOrDefault(x => x.ID == layerID);

                    if (layer == null)
                        layer = state.UserLayers.FirstOrDefault(x => x.InheritedID == layerID);

                    if (layer != null)
                        layerID = layer.ID;

                    state.SelectedUserLayerIDs.Add(layerID);

                    var visibleLocations = await fetchVisibleUserLocations(details.Username, details.EnterpriseAPIKey, state.SelectedUserLayerIDs);

                    state.VisibleUserLocations = limitUserLocationsGeographically(visibleLocations, userMap.Coordinates);

                    state.VisibleUserLocations = state.VisibleUserLocations.Distinct().ToList();
                }
            }

            var userLayer = state.UserLayers.Where(x => x.Title == "User").FirstOrDefault();

            var userLayerID = (userLayer == null) ? Guid.Empty : userLayer.ID;

            state.UserTopLists = await fetchUserTopLists(details.Username, details.EnterpriseAPIKey, userLayerID);

            state.Loading = false;

            return state;
        }

        public virtual async Task LoadCuratedLocationsIntoDB(string json, Guid layerID)
        {
            var list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(json);

            float testFloat = 0;

            var workingList = list.Where(x => x.Latitude != null && float.TryParse(x.Latitude.ToString(), out testFloat)
                && x.Longitude != null && float.TryParse(x.Longitude.ToString(), out testFloat)).ToList();

            workingList.ForEach(
           async (jsonLocation) =>
           {
               var location = new UserLocation()
               {
                   Address = jsonLocation.Address,
                   Country = jsonLocation.Country,
                   Icon = jsonLocation.Icon,
                   Instagram = jsonLocation.Instagram,
                   Latitude = jsonLocation.Latitude,
                   LayerID = layerID,
                   Longitude = jsonLocation.Longitude,
                   State = jsonLocation.State,
                   Telephone = jsonLocation.Telephone,
                   Title = jsonLocation.Title,
                   Town = jsonLocation.Town,
                   Website = jsonLocation.Website,
                   ZipCode = jsonLocation.Zipcode
               };

               var resp = amblGraph.AddLocation("default@amblon.com", details.EnterpriseAPIKey, location);
           });
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
        protected virtual Tuple<float, float> computeCenter(float lat1, float long1, float lat2, float long2)
        {
            //var latRad1 = Math.PI * lat1 / 180.0;
            //var latRad2 = Math.PI * lat2 / 180.0;
            //var longRad1 = Math.PI * long1 / 180.0;
            //var longRad2 = Math.PI * long2 / 180.0;

            //var dLon = (longRad2-longRad1); 


            //var Bx = Math.Cos(latRad2) * Math.Cos(dLon);
            //var By = Math.Cos(latRad2) * Math.Sin(dLon);
            //var avgLat = Math.Atan2(
            //        Math.Sin(latRad1) + 
            //        Math.Sin(latRad2), 
            //        Math.Sqrt((
            //        Math.Cos(latRad1)+Bx) * (Math.Cos(latRad1)+Bx) + By*By));

            //var avgLong = longRad1 + Math.Atan2(By, Math.Cos(latRad1) + Bx);

            //return new Tuple<float, float>(float.Parse(avgLat.ToString()), float.Parse(avgLong.ToString()));

            return new Tuple<float, float>((lat1 + lat2) / 2, (long1 + long2) / 2);
        }

        protected virtual float computeSearchRadius(float lat1, float long1, float lat2, float long2)
        {
            var coord1 = new GeoCoordinate(Convert.ToDouble(lat1), Convert.ToDouble(long1));
            var coord2 = new GeoCoordinate(Convert.ToDouble(lat2), Convert.ToDouble(long2));

            var distanceMeters = Math.Abs(coord1.GetDistanceTo(coord2));

            return float.Parse((distanceMeters / 1609.344).ToString());
        }

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

            if (state.LocalSearchUserLocations == null)
                state.LocalSearchUserLocations = new List<UserLocation>();

            if (state.OtherSearchUserLocations == null)
                state.OtherSearchUserLocations = new List<UserLocation>();

            if (state.UserAlbums == null)
                state.UserAlbums = new List<UserAlbum>();

            if (state.UserItineraries == null)
                state.UserItineraries = new List<UserItinerary>();

            state.UserTopLists = state.UserTopLists ?? new List<UserTopList>();
        }

        protected virtual async Task<List<UserAccolade>> fetchUserAccolades(string email, string entAPIKey, Guid layerId)
        {
            var userAccolades = new List<UserAccolade>();

            var accolades = await amblGraph.ListAccolades(email, entAPIKey, layerId);

            accolades.ForEach(
                (accolade) =>
                {
                    userAccolades.Add(mapUserAccolade(accolade));
                });

            return userAccolades;
        }

        protected virtual async Task<List<UserAlbum>> fetchUserAlbums(string email, string entAPIKey)
        {
            var userAlbums = new List<UserAlbum>();

            var albums = await amblGraph.ListAlbums(email, entAPIKey);

            albums.ForEach(
                (album) =>
                {
                    var photos = amblGraph.ListPhotos(email, entAPIKey, album.ID).GetAwaiter().GetResult();
                    userAlbums.Add(mapUserAlbum(album, photos));
                });

            return userAlbums;
        }

        protected virtual async Task<List<UserItinerary>> fetchUserItineraries(string email, string entAPIKey)
        {
            var userItineraries = new List<UserItinerary>();

            var itineraries = await amblGraph.ListItineraries(email, entAPIKey);

            itineraries.ForEach(
                async (itinerary) =>
                {
                    var itineraryActivities = await amblGraph.ListItineraryActivities(email, entAPIKey, itinerary.ID);
                    userItineraries.Add(mapUserItinerary(itinerary, itineraryActivities));
                });

            return userItineraries;
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

                    // Insert curated layer first
                    userLayers.Insert(0, mapUserLayer(layerInfo.Item1, layerInfo.Item2, coords));
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

        protected virtual async Task<List<UserLocation>> fetchVisibleUserLocations(string email, string entAPIKey, List<Guid> layerIDs)
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
                            var loc = mapUserLocation(location, layerID, state.UserLayers.Any(x => x.ID == layerID && !x.Shared));
                            userLocations.Add(loc);
                        });

                    if (layer != null && layer.Coordinates != null)
                        userLocations = limitUserLocationsGeographically(userLocations, layer.Coordinates);
                });

            return userLocations;
        }

        protected virtual async Task<List<UserTopList>> fetchUserTopLists(string email, string entAPIKey, Guid layerId)
        {
            var userTopLists = new List<UserTopList>();

            var topLists = await amblGraph.ListTopLists(email, entAPIKey);

            topLists.ForEach(
                (topList) =>
                {
                    var locations = amblGraph.ListTopListLocations(email, entAPIKey, topList.ID).GetAwaiter().GetResult();
                    userTopLists.Add(mapUserTopList(topList, locations, layerId));
                });

            return userTopLists;

        }


        protected virtual List<UserLocation> limitUserLocationsByRadius(List<UserLocation> userLocations, float radius, float centerLat, float centerLong)
        {
            if (radius > 0)
            {
                var center = new GeoCoordinate(Convert.ToDouble(centerLat), Convert.ToDouble(centerLong));

                return userLocations.Where(x =>
                    {
                        var coord = new GeoCoordinate(Convert.ToDouble(x.Latitude), Convert.ToDouble(x.Longitude));

                        return float.Parse((Math.Abs(coord.GetDistanceTo(center)) / 1609.344).ToString()) <= radius;
                    }).ToList();
            }
            else
                return userLocations;
        }

        protected virtual List<UserLocation> limitUserLocationsBySearch(List<UserLocation> userLocations, string searchTerm)
        {
            if (!String.IsNullOrEmpty(searchTerm))
            {
                return userLocations.Where(x => x.Title.ToLower().Contains(searchTerm.ToLower())).ToList();
            }
            else
                return userLocations;
        }

        protected virtual List<UserLocation> limitUserLocationsGeographically(List<UserLocation> userLocations, float[] coordinates)
        {
            if (coordinates != null && coordinates.Count() == 4)
            {
                return userLocations.Where(x => x.Latitude <= coordinates[0]
                                    && x.Latitude >= coordinates[2]
                                    && x.Longitude <= coordinates[1]
                                    && x.Longitude >= coordinates[3]).ToList();
            }
            else
                return userLocations;
        }

        protected virtual List<UserPhoto> mapImageDataToUserPhotos(List<UserPhoto> photos, List<ImageMessage> images)
        {
            var photoCount = 0;

            photos.ForEach(
                (photo) =>
                {
                    var img = images.FirstOrDefault(x => QueryHelpers.ParseQuery(x.Headers)["ID"] == photo.ID);

                    if (img == null)
                        img = images[photoCount];

                    if (img != null)
                        photo.ImageData = img;

                    photoCount++;
                });

            return photos;
        }

        protected virtual UserAccolade mapUserAccolade(Accolade accolade)
        {
            return new UserAccolade()
            {
                ID = accolade.ID,
                LocationID = accolade.LocationID,
                Rank = accolade.Rank,
                Title = accolade.Title,
                Year = accolade.Year
            };
        }

        protected virtual UserAlbum mapUserAlbum(Album album, List<Photo> photos)
        {
            var userAlbum = new UserAlbum()
            {
                ID = album.ID,
                Photos = new List<UserPhoto>(),
                Title = album.Title
            };

            photos.ForEach(
                (photo) =>
                {
                    var userPhoto = mapUserPhoto(photo);
                    userAlbum.Photos.Add(userPhoto);
                });

            return userAlbum;
        }

        protected virtual UserItinerary mapUserItinerary(Itinerary itinerary, List<ItineraryActivity> itineraryActivities)
        {
            var userItinerary = new UserItinerary()
            {
                ID = itinerary.ID,
                Activities = new List<UserItineraryActivity>(),
                EndDate = itinerary.EndDate,
                StartDate = itinerary.StartDate,
                Title = itinerary.Title,
                CreatedDateTime = itinerary.CreatedDateTime
            };

            itineraryActivities.ForEach(
                (itineraryActivity) =>
                {
                    var userItineryActivity = mapUserItineraryActivity(itineraryActivity);
                    userItinerary.Activities.Add(userItineryActivity);
                });

            return userItinerary;
        }

        protected virtual UserItineraryActivity mapUserItineraryActivity(ItineraryActivity itineraryActivity)
        {
            return new UserItineraryActivity()
            {
                ID = itineraryActivity.ID,
                ActivityName = itineraryActivity.ActivityName,
                StartDateTime = itineraryActivity.StartDateTime,
                EndDateTime = itineraryActivity.EndDateTime,
                LocationID = itineraryActivity.LocationID,
                CreatedDateTime = itineraryActivity.CreatedDateTime
            };
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
                GoogleLocationName = location.GoogleLocationName,
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
            };
        }

        protected virtual UserMap mapUserMap(Map map)
        {
            bool hasCoords = false;
            var coords = map.Coordinates.Split(",");
            var fCoords = new float[4];

            if (coords.Count() == 4)
            {
                hasCoords = true;
                var count = 0;

                coords.ToList().ForEach(
                    (coord) =>
                    {
                        fCoords[count] = float.Parse(coord);
                        count++;
                    });
            }

            return new UserMap()
            {
                ID = map.ID,
                Coordinates = hasCoords ? fCoords : null,
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
            bool hasCoords = false;
            var coords = parent.Coordinates.Split(",");
            var fCoords = new float[4];

            if (coords.Count() == 4)
            {
                hasCoords = true;
                var count = 0;

                coords.ToList().ForEach(
                    (coord) =>
                    {
                        fCoords[count] = float.Parse(coord);
                        count++;
                    });
            }

            return new UserMap()
            {
                ID = map.ID,
                Coordinates = hasCoords ? fCoords : null,
                DefaultLayerID = parent.DefaultLayerID,
                Deletable = map.Deletable,
                Latitude = parent.Latitude,
                Longitude = parent.Longitude,
                Primary = map.Primary,
                Shared = true,
                Title = map.Title,
                Zoom = parent.Zoom,
                InheritedID = parent.ID
            };
        }

        protected virtual UserPhoto mapUserPhoto(Photo photo)
        {
            return new UserPhoto()
            {
                ID = photo.ID,
                Caption = photo.Caption,
                URL = photo.URL,
                LocationID = photo.LocationID
            };
        }

        protected virtual UserTopList mapUserTopList(TopList topList, List<Location> locations, Guid layerId)
        {
            var userTopList = new UserTopList()
            {
                ID = topList.ID,
                LocationList = new List<UserLocation>(),
                Title = topList.Title,
                OrderedValue = topList.OrderedValue
            };

            locations.ForEach(
                (location) =>
                {
                    var userLocation = mapUserLocation(location, layerId, true);
                    userTopList.LocationList.Add(userLocation);
                });

            return userTopList;
        }

        protected virtual List<UserLocation> removeUserLocationsByLayerID(List<UserLocation> userLocations, Guid layerID)
        {
            return userLocations.Where(x => x.LayerID != layerID).ToList();
        }
        #endregion
    }
}