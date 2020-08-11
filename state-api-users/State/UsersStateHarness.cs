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

namespace AmblOn.State.API.Users.State
{
    public class UsersStateHarness : LCUStateHarness<UsersState>
    {
        #region Constants
        #endregion

        #region Fields 
        #endregion

        #region Properties 
        #endregion

        #region Constructors
        public UsersStateHarness(UsersState state)
            : base(state ?? new UsersState())
        { }
        #endregion

        #region API Methods

        #region Add
        // public virtual async Task AddAccolade(AmblOnGraph amblGraph, string username, string entApiKey, UserAccolade accolade, Guid locationId)
        // {
        //     ensureStateObject();

        //     var accoladeResp = await amblGraph.AddAccolade(username, entApiKey, accolade, locationId);

        //     if (accoladeResp.Status)
        //     {
        //         accolade.ID = accoladeResp.Model;

        //         if (!State.UserAccolades.Any(x => x.ID == accolade.ID))
        //             State.UserAccolades.Add(accolade);
        //     }

        //     State.Loading = false;
        // }

        public virtual async Task AddAlbum(EnterpriseManagerClient entMgr, ApplicationManagerClient appMgr, AmblOnGraph amblGraph, string username, string entApiKey, string appId,
            UserAlbum album, List<ImageMessage> images)
        {
            ensureStateObject();

            album.Photos = mapImageDataToUserPhotos(album.Photos, images);

            var albumResp = await amblGraph.AddAlbum(username, entApiKey, album);

            if (albumResp.Status)
            {
                album.ID = albumResp.Model;

                if (!State.UserAlbums.Any(x => x.ID == album.ID))
                    State.UserAlbums.Add(album);

                if (album.Photos.Count > 0)
                {
                    await album.Photos.Each(async (photo) =>
                    {
                        await AddPhoto(entMgr, appMgr, amblGraph, username, entApiKey, appId, photo,
                            album.ID.HasValue ? album.ID.Value : Guid.Empty);
                    });
                }
            }

            State.UserAlbums = await fetchUserAlbums(amblGraph, username, entApiKey);

            State.Loading = false;
        }

        public virtual async Task AddItinerary(AmblOnGraph amblGraph, string username, string entApiKey, Itinerary itinerary)
        {
            ensureStateObject();

            itinerary.CreatedDateTime = DateTime.Now;
            itinerary.Shared = false;
            itinerary.Editable = true;

            var itineraryResp = await amblGraph.AddItinerary(username, entApiKey, itinerary);

            if (itineraryResp.Status)
            {
                itinerary.ID = itineraryResp.Model;

                await itinerary.ActivityGroups.Each(async (activityGroup) =>
                {
                    activityGroup.CreatedDateTime = DateTime.Now;

                    var activityGroupResp = await amblGraph.AddActivityGroup(username, entApiKey, itinerary.ID.Value, activityGroup);

                    if (activityGroupResp.Status)
                    {
                        activityGroup.ID = activityGroupResp.Model;

                        await activityGroup.Activities.Each(async (activity) =>
                        {
                            activity.CreatedDateTime = DateTime.Now;

                            var activityResp = await amblGraph.AddActivityToAG(username, entApiKey, itinerary.ID.Value, activityGroup.ID.Value, activity);

                            if (activityResp.Status)
                            {
                                activity.ID = activityResp.Model;
                            }
                        });
                    }
                });

                if (!State.UserItineraries.Any(x => x.ID == itinerary.ID))
                    State.UserItineraries.Add(itinerary);
            }

            State.Loading = false;
        }

        // public virtual async Task AddLocation(AmblOnGraph amblGraph, string username, string entApiKey, UserLocation location)
        // {
        //     ensureStateObject();

        //     if (State.UserLayers.Any(x => x.ID == location.LayerID && !x.Shared))
        //     {
        //         var locationResp = await amblGraph.AddLocation(username, entApiKey, location);

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

        // public virtual async Task AddMap(AmblOnGraph amblGraph, string username, string entApiKey, UserMap map)
        // {
        //     ensureStateObject();

        //     BaseResponse<Guid> mapResp = new BaseResponse<Guid>() { Status = Status.Initialized };

        //     if (!map.Shared)
        //         mapResp = await amblGraph.AddMap(username, entApiKey, map);
        //     else
        //         mapResp = await amblGraph.AddSharedMap(username, entApiKey, map, (map.InheritedID.HasValue ? map.InheritedID.Value : Guid.Empty));

        //     if (mapResp.Status)
        //     {
        //         map.ID = mapResp.Model;

        //         State.UserMaps.Add(map);

        //         State.UserMaps = State.UserMaps.Distinct().ToList();

        //         State.SelectedUserMapID = map.ID.Value;
        //     }

        //     State.Loading = false;
        // }

        public virtual async Task AddPhoto(EnterpriseManagerClient entMgr, ApplicationManagerClient appMgr, AmblOnGraph amblGraph, string username, string entApiKey, string appId, 
            UserPhoto photo, Guid albumID)
        {
            ensureStateObject();

            var ent = await entMgr.GetEnterprise(entApiKey);

            if(photo.ImageData != null){
                var index = photo.ImageData.DataString.IndexOf(',');

                photo.ImageData.DataString = photo.ImageData.DataString.Substring(index + 1);

                photo.ImageData.Data = Convert.FromBase64String(photo.ImageData.DataString);

                await appMgr.SaveFile(photo.ImageData.Data, ent.Model.ID, "", QueryHelpers.ParseQuery(photo.ImageData.Headers)["filename"], 
                    new Guid(appId), "admin/" + username + "/albums/" + albumID.ToString());

                photo.URL = "/" + ent.Model.ID + "/" + appId + "/admin/" + username + "/albums/" + albumID.ToString() + "/" + QueryHelpers.ParseQuery(photo.ImageData.Headers)["filename"];

                photo.ImageData = null;

                var photoResp = await amblGraph.AddPhoto(username, entApiKey, photo, albumID);

                if (photoResp.Status)
                {
                    photo.ID = photoResp.Model;
                }

                State.UserAlbums = await fetchUserAlbums(amblGraph, username, entApiKey);
            }

            State.Loading = false;
        }

        public virtual async Task AddPhoto(EnterpriseManagerClient entMgr, ApplicationManagerClient appMgr, AmblOnGraph amblGraph, string username, string entApiKey, string appId, 
            List<ImageMessage> images, UserAlbum album)
        {
            ensureStateObject();

            album.Photos = mapImageDataToUserPhotos(album.Photos, images);

            var ent = await entMgr.GetEnterprise(entApiKey);

            await album.Photos.Each(async (photo) =>{
                if (photo.ImageData != null){
                    var index = photo.ImageData.DataString.IndexOf(',');

                    photo.ImageData.DataString = photo.ImageData.DataString.Substring(index + 1);

                    photo.ImageData.Data = Convert.FromBase64String(photo.ImageData.DataString);

                    await appMgr.SaveFile(photo.ImageData.Data, ent.Model.ID, "", QueryHelpers.ParseQuery(photo.ImageData.Headers)["filename"], 
                        new Guid(appId), "admin/" + username + "/albums/" + album.ID.ToString());

                    photo.URL = "/" + ent.Model.ID + "/" + appId + "/admin/" + username + "/albums/" + album.ID.ToString() + "/" + QueryHelpers.ParseQuery(photo.ImageData.Headers)["filename"];

                    photo.ImageData = null;

                    var photoResp = await amblGraph.AddPhoto(username, entApiKey, photo, album.ID.Value);

                    if (photoResp.Status)
                    {
                        photo.ID = photoResp.Model;
                    }             
                }

            });

            State.UserAlbums = await fetchUserAlbums(amblGraph, username, entApiKey);

            State.Loading = false;
        }

        // public virtual async Task AddSelectedLayer(AmblOnGraph amblGraph, string username, string entApiKey, Guid layerID)
        // {
        //     ensureStateObject();

        //     if (State.UserLayers.Any(x => x.ID == layerID))
        //         State.SelectedUserLayerIDs.Add(layerID);

        //     //TODO: Check for whether locations are in AllLocations
        //     var locationsToAdd = await fetchVisibleUserLocations(amblGraph, username, entApiKey, new List<Guid>() { layerID });

        //     State.AllUserLocations.AddRange(locationsToAdd);

        //     State.AllUserLocations = State.AllUserLocations.Distinct().ToList();

        //     var userMap = State.UserMaps.FirstOrDefault(x => x.ID == State.SelectedUserMapID);

        //     if (userMap != null)
        //     {
        //         State.VisibleUserLocations.AddRange(limitUserLocationsGeographically(locationsToAdd, userMap.Coordinates));

        //         State.VisibleUserLocations = State.VisibleUserLocations.Distinct().ToList();
        //     }

        //     State.Loading = false;
        // }

        public virtual async Task AddTopList(AmblOnGraph amblGraph, string username, string entApiKey, UserTopList topList)
        {
            ensureStateObject();

            var topListResp = await amblGraph.AddTopList(username, entApiKey, topList);

            if (topListResp.Status)
            {
                topList.ID = topListResp.Model;

                if (!State.UserTopLists.Any(x => x.ID == topList.ID))
                    State.UserTopLists.Add(topList);
            }

            State.Loading = false;
        }

        public virtual async Task AddUserInfo(AmblOnGraph amblGraph, string username, string entApiKey, UserInfo userInfo)
        {
            ensureStateObject();

            var userInfoResp = await amblGraph.AddUserInfo(username, entApiKey, userInfo);

            if (userInfoResp.Status)
            {
                userInfo.ID = userInfoResp.Model;

                State.UserInfo = userInfo;
            }

            State.Loading = false;
        }
        #endregion

        // public virtual async Task ChangeViewingArea(float[] coordinates)
        // {
        //     ensureStateObject();

        //     var userMap = State.UserMaps.FirstOrDefault(x => x.ID == State.SelectedUserMapID);

        //     if (userMap != null)
        //     {
        //         userMap.Coordinates = coordinates;

        //         //TODO : Does this need to be reloaded
        //         //var visibleLocations = await fetchVisibleUserLocations(username, entApiKey, State.SelectedUserLayerIDs);

        //         State.VisibleUserLocations = limitUserLocationsGeographically(State.AllUserLocations, userMap.Coordinates)
        //                                     .Distinct()
        //                                     .ToList();

        //         //State.VisibleUserLocations = State.VisibleUserLocations.Distinct().ToList();
        //     }

        //     State.Loading = false;
        // }

        // public virtual async Task ChangeExcludedCurations(AmblOnGraph amblGraph, string username, string entApiKey, ExcludedCurations curations)
        // {
        //     ensureStateObject();

        //     State.ExcludedCuratedLocations = curations;

        //     await amblGraph.EditExcludedCurations(username, entApiKey, curations);
        // }

        #region Delete
        // public virtual async Task DeleteAccolades(AmblOnGraph amblGraph, string username, string entApiKey, Guid[] accoladeIDs, Guid locationId)
        // {
        //     ensureStateObject();

        //     var accoladeResp = await amblGraph.DeleteAccolades(username, entApiKey, accoladeIDs, locationId);

        //     if (accoladeResp.Status)
        //     {
        //         State.UserAccolades.RemoveAll(x => accoladeIDs.ToList<Guid>().Contains(x.ID ?? Guid.Empty));

        //         State.UserAccolades = State.UserAccolades.Distinct().ToList();
        //     }

        //     State.Loading = false;
        // }

        public virtual async Task DeleteAlbum(AmblOnGraph amblGraph, string username, string entApiKey, Guid albumID)
        {
            ensureStateObject();

            var albumResp = await amblGraph.DeleteAlbum(username, entApiKey, albumID);

            if (albumResp.Status)
            {
                var existing = State.UserAlbums.FirstOrDefault(x => x.ID == albumID);

                if (existing != null)
                    State.UserAlbums.Remove(existing);

                State.UserAlbums = State.UserAlbums.Distinct().ToList();
            }

            State.Loading = false;
        }

        public virtual async Task DeleteItineraries(AmblOnGraph amblGraph, string username, string entApiKey, List<Guid> itineraryIDs)
        {
            ensureStateObject();

            var success = true;

            await itineraryIDs.Each(async (itineraryID) =>
            {
                var itinerary = State.UserItineraries.FirstOrDefault(x => x.ID == itineraryID);

                if (itinerary != null)
                {
                    await itinerary.ActivityGroups.Each(async (activityGroup) =>
                    {
                        await activityGroup.Activities.Each(async (activity) =>
                        {
                            var actResp = await amblGraph.DeleteActivity(username, entApiKey, itinerary.ID.Value, activityGroup.ID.Value, activity.ID.Value);

                            if (!actResp.Status)
                                success = false;
                        });

                        if (success)
                        {
                            var actGroupResp = await amblGraph.DeleteActivityGroup(username, entApiKey, itinerary.ID.Value, activityGroup.ID.Value);

                            if (!actGroupResp.Status)
                                success = false;
                        }
                    });

                    if (success)
                    {
                        var itineraryResp = await amblGraph.DeleteItinerary(username, entApiKey, itineraryID);

                        if (!itineraryResp.Status)
                            success = false;
                    }

                    if (success)
                    {
                        var existing = State.UserItineraries.FirstOrDefault(x => x.ID == itineraryID);

                        if (existing != null)
                            State.UserItineraries.Remove(existing);

                        State.UserItineraries = State.UserItineraries.Distinct().ToList();
                    }
                }
                else
                    success = false;
            });

            if (!success)
                State.Error = "General Error";

            State.Loading = false;
        }

        public virtual async Task DeleteLocation(AmblOnGraph amblGraph, string username, string entApiKey, Guid locationID)
        {
            ensureStateObject();

            var locationResp = await amblGraph.DeleteLocation(username, entApiKey, locationID);

            if (locationResp.Status)
            {
                var existingVisible = State.VisibleUserLocations.FirstOrDefault(x => x.ID == locationID);

                if (existingVisible != null)
                {
                    State.VisibleUserLocations.Remove(existingVisible);
                    State.AllUserLocations.RemoveAll(item => item.ID == existingVisible.ID);
                }

                State.VisibleUserLocations = State.VisibleUserLocations.Distinct().ToList();
            }

            State.Loading = false;
        }

        // public virtual async Task DeleteMap(AmblOnGraph amblGraph, string username, string entApiKey, Guid mapID)
        // {
        //     ensureStateObject();

        //     var userMap = State.UserMaps.FirstOrDefault(x => x.ID == mapID);

        //     if (userMap != null && userMap.Deletable)
        //     {
        //         BaseResponse mapResp = new BaseResponse() { Status = Status.Initialized };

        //         if (!userMap.Shared)
        //             mapResp = await amblGraph.DeleteMap(username, entApiKey, mapID);
        //         else
        //             //mapResp = await amblGraph.DeleteSharedMap(username, entApiKey, mapID);

        //         if (mapResp.Status)
        //         {
        //             var existingMap = State.UserMaps.FirstOrDefault(x => x.ID == mapID);

        //             if (existingMap != null)
        //                 State.UserMaps.Remove(existingMap);

        //             State.UserMaps = State.UserMaps.Distinct().ToList();

        //             if (!State.UserMaps.Any(x => x.Primary == true))
        //             {
        //                 var newPrimary = State.UserMaps.FirstOrDefault(x => x.Shared && !x.Deletable);

        //                 if (newPrimary != null)
        //                     newPrimary.Primary = true;
        //                 else if (State.UserMaps.Count > 0)
        //                     State.UserMaps.First().Primary = true;
        //             }

        //             if (State.UserMaps.Any(x => x.Primary))
        //             {
        //                 var primaryMap = State.UserMaps.First(x => x.Primary);

        //                 State.SelectedUserMapID = (primaryMap.ID.HasValue ? primaryMap.ID.Value : Guid.Empty);

        //                 // TODO:  Should layers and locations be reloaded, or loaded from local collection
        //                 State.SelectedUserLayerIDs.Clear();

        //                 State.SelectedUserLayerIDs.Add(primaryMap.DefaultLayerID);

        //                 //var visibleLocations = await fetchVisibleUserLocations(username, entApiKey, State.SelectedUserLayerIDs);

        //                 State.VisibleUserLocations = limitUserLocationsGeographically(State.AllUserLocations, primaryMap.Coordinates);

        //                 State.VisibleUserLocations = State.VisibleUserLocations.Distinct().ToList();
        //             }
        //         }
        //     }

        //     State.Loading = false;
        // }

        // public virtual async Task DeleteMaps(AmblOnGraph amblGraph, string username, string entApiKey, Guid[] mapIDs)
        // {
        //     ensureStateObject();

        //     var mapResp = await amblGraph.DeleteMaps(username, entApiKey, mapIDs);

        //     if (mapResp.Status)
        //     {
        //         State.UserMaps.RemoveAll(x => mapIDs.ToList().Contains(x.ID ?? default(Guid)));

        //         State.UserMaps = State.UserMaps.Distinct().ToList();

        //         if (!State.UserMaps.Any(x => x.Primary == true))
        //         {
        //             var newPrimary = State.UserMaps.FirstOrDefault(x => x.Shared && !x.Deletable);

        //             if (newPrimary != null)
        //                 newPrimary.Primary = true;
        //             else if (State.UserMaps.Count > 0)
        //                 State.UserMaps.First().Primary = true;
        //         }

        //         if (State.UserMaps.Any(x => x.Primary))
        //         {
        //             var primaryMap = State.UserMaps.First(x => x.Primary);

        //             State.SelectedUserMapID = (primaryMap.ID.HasValue ? primaryMap.ID.Value : Guid.Empty);

        //             // TODO: Do layers need to be reloaded if a map is removed
        //             State.SelectedUserLayerIDs.Clear();

        //             State.SelectedUserLayerIDs.Add(primaryMap.DefaultLayerID);

        //             // TODO: Reload from a local collection instead
        //             //var visibleLocations = await fetchVisibleUserLocations(username, entApiKey, State.SelectedUserLayerIDs);

        //             State.VisibleUserLocations = limitUserLocationsGeographically(State.AllUserLocations, primaryMap.Coordinates)
        //                                         .Distinct()
        //                                         .ToList();

        //             //State.VisibleUserLocations = State.VisibleUserLocations.Distinct().ToList();
        //         }
        //     }


        //     State.Loading = false;
        // }

        public virtual async Task DeletePhoto(AmblOnGraph amblGraph, string username, string entApiKey, Guid photoID)
        {
            ensureStateObject();

            var photoResp = await amblGraph.DeletePhoto(username, entApiKey, photoID);

            if (photoResp.Status)
            {
                var existingAlbum = State.UserAlbums.FirstOrDefault(x => x.Photos.Any(y => y.ID == photoID));

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

            State.Loading = false;
        }

        public virtual async Task DeleteTopList(AmblOnGraph amblGraph, string username, string entApiKey, Guid topListID)
        {
            ensureStateObject();

            var topListResp = await amblGraph.DeleteTopList(username, entApiKey, topListID);

            if (topListResp.Status)
            {
                var existing = State.UserTopLists.FirstOrDefault(x => x.ID == topListID);

                if (existing != null)
                    State.UserTopLists.Remove(existing);

                State.UserTopLists = State.UserTopLists.Distinct().ToList();
            }

            State.Loading = false;
        }

        public virtual async Task DedupLocationsByMap(AmblOnGraph amblGraph, string username, string entApiKey, Guid mapID)
        {
            ensureStateObject();

            var locationResp = await amblGraph.DedupLocationsByMap(username, entApiKey, mapID);

            // Do not refresh state for now


            State.Loading = false;
        }
        #endregion

        #region Edit
        // public virtual async Task EditAccolade(AmblOnGraph amblGraph, string username, string entApiKey, UserAccolade accolade, Guid locationId)
        // {
        //     ensureStateObject();

        //     var existing = State.UserAccolades.FirstOrDefault(x => x.ID == accolade.ID);

        //     if (existing != null)
        //     {
        //         var accoladeResp = await amblGraph.EditAccolade(username, entApiKey, accolade, locationId);

        //         if (accoladeResp.Status)
        //         {

        //             State.UserAccolades.Remove(existing);

        //             State.UserAccolades.Add(accolade);

        //             State.UserAccolades = State.UserAccolades.Distinct().ToList();
        //         }
        //     }

        //     State.Loading = false;
        // }

        public virtual async Task EditAlbum(AmblOnGraph amblGraph, string username, string entApiKey, UserAlbum album)
        {
            ensureStateObject();

            var existing = State.UserAlbums.FirstOrDefault(x => x.ID == album.ID);

            if (existing != null)
            {
                var albumResp = await amblGraph.EditAlbum(username, entApiKey, album);

                if (albumResp.Status)
                {
                    State.UserAlbums.Remove(existing);

                    State.UserAlbums.Add(album);

                    State.UserAlbums = State.UserAlbums.Distinct().ToList();
                }
            }

            State.Loading = false;
        }

        public virtual async Task EditItinerary(AmblOnGraph amblGraph, AmblOnGraphFactory amblGraphFactory, string username, string entApiKey, Itinerary itinerary, List<ActivityLocationLookup> activityLocations)
        {
            ensureStateObject();

            var activitiesList = new List<Activity>();

            if(!activityLocations.IsNullOrEmpty()){       
                activitiesList =  await addLocationFromActivity(amblGraph, username, entApiKey, activityLocations);           
            }

            var existing = State.UserItineraries.FirstOrDefault(x => x.ID == itinerary.ID);

            if (existing != null)
            {
                if (existing.Editable)
                {
                    var success = true;

                    var itineraryResp = await amblGraph.EditItinerary(username, entApiKey, itinerary);

                    if (!itineraryResp.Status)
                        success = false;

                    if (success)
                    {
                        await itinerary.ActivityGroups.Each(async (activityGroup) =>
                        {
                            var agExisting = existing.ActivityGroups.FirstOrDefault(x => x.ID == activityGroup.ID);

                            if (agExisting == null)
                            {
                                var addActGResp = await amblGraph.AddActivityGroup(username, entApiKey, itinerary.ID.Value, activityGroup);

                                if (addActGResp.Status)
                                {
                                    activityGroup.ID = addActGResp.Model;

                                    await activityGroup.Activities.Each(async (activity) =>
                                    {
                                        var addActResp = new BaseResponse<Guid>();
                                        
                                        if(activity.ID == null){
                                            addActResp = await amblGraph.AddActivityToAG(username, entApiKey, itinerary.ID.Value, activityGroup.ID.Value, activity);

                                            activity.ID = addActResp.Model;

                                            var exists = activitiesList?.FirstOrDefault(x => x.Title == activity.Title);

                                            if(exists != null){
                                                exists.ID = activity.ID;
                                         
                                                addActResp = await amblGraph.AddActivityToAG(username, entApiKey, itinerary.ID.Value, activityGroup.ID.Value, exists);
                                            }
                                        }

                                        else{
                                            var exists = activitiesList?.FirstOrDefault(x => x.ID == activity.ID);

                                            addActResp = await amblGraph.AddActivityToAG(username, entApiKey, itinerary.ID.Value, activityGroup.ID.Value, exists);
                                        }
                                        
                                        activity.ID = addActResp.Model;

                                        if (!addActResp.Status)
                                            success = false;
                                    });
                                }
                                else
                                    success = false;
                            }
                            else
                            {
                                await activityGroup.Activities.Each(async (activity) =>
                                {
                                    var aExisting = agExisting.Activities.FirstOrDefault(x => x.ID == activity.ID);

                                    if (aExisting == null)
                                    {
                                        var exists = activitiesList?.FirstOrDefault(x => x.ID == activity.ID);

                                        var addActResp = await amblGraph.AddActivityToAG(username, entApiKey, itinerary.ID.Value, activityGroup.ID.Value, exists ?? activity);

                                        activity.ID = addActResp.Model;

                                        if (!addActResp.Status)
                                            success = false;
                                    }
                                    else
                                    {
                                        var exists = activitiesList?.FirstOrDefault(x => x.ID == activity.ID);

                                        var editActResp = await amblGraph.EditActivity(username, entApiKey, exists ?? activity);

                                        if (!editActResp.Status)
                                            success = false;
                                    }
                                });

                                var editActGResp = await amblGraph.EditActivityGroup(username, entApiKey, activityGroup);

                                if (!editActGResp.Status)
                                    success = false;
                            }
                        });

                        await existing.ActivityGroups.Each(async (activityGroup) =>
                        {
                            var agNew = itinerary.ActivityGroups.FirstOrDefault(x => x.ID == activityGroup.ID);

                            if (agNew == null)
                            {
                                await activityGroup.Activities.Each(async (activity) =>
                                {
                                    var delActResp = await amblGraph.DeleteActivity(username, entApiKey, itinerary.ID.Value, activityGroup.ID.Value, activity.ID.Value);

                                    if (!delActResp.Status)
                                        success = false;
                                });

                                if (success)
                                {
                                    var delActGResp = await amblGraph.DeleteActivityGroup(username, entApiKey, itinerary.ID.Value, activityGroup.ID.Value);

                                    if (!delActGResp.Status)
                                        success = false;
                                }
                            }
                            else
                            {
                                await activityGroup.Activities.Each(async (activity) =>
                                {
                                    var aNew = agNew.Activities.FirstOrDefault(x => x.ID == activity.ID);

                                    if (aNew == null)
                                    {
                                        var delActResp = await amblGraph.DeleteActivity(username, entApiKey, itinerary.ID.Value, activityGroup.ID.Value, activity.ID.Value);

                                        if (!delActResp.Status)
                                            success = false;
                                    }
                                });
                            }
                        });
                    }

                    if (success)
                        State.UserItineraries = await fetchUserItineraries(amblGraph, username, entApiKey);
                    else
                        State.Error = "General Error updating user itinerary.";
                }
                else State.Error = "Cannot edit a shared itinerary.";

            }
            else
                State.Error = "Itinerary not found.";

            State.Loading = false;
        }

        // public virtual async Task EditLocation(AmblOnGraph amblGraph, string username, string entApiKey, UserLocation location)
        // {
        //     ensureStateObject();

        //     if (State.UserLayers.Any(x => x.ID == location.LayerID && !x.Shared))
        //     {
        //         var locationResp = await amblGraph.EditLocation(username, entApiKey, location);

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

        public virtual async Task EditMap(AmblOnGraph amblGraph, string username, string entApiKey, UserMap map)
        {
            ensureStateObject();

            var userMap = State.UserMaps.FirstOrDefault(x => x.ID == map.ID);

            BaseResponse mapResp = new BaseResponse() { Status = Status.Initialized };

            if (userMap != null && !userMap.Shared)
                mapResp = await amblGraph.EditMap(username, entApiKey, map);
            else if (userMap != null)
                //mapResp = await amblGraph.EditSharedMap(username, entApiKey, map);

            if (mapResp.Status)
            {
                var existingMap = State.UserMaps.FirstOrDefault(x => x.ID == map.ID);

                if (existingMap != null)
                {
                    State.UserMaps.Remove(existingMap);

                    State.UserMaps.Add(map);

                    State.UserMaps = State.UserMaps.Distinct().ToList();
                }
            }

            State.Loading = false;
        }

        public virtual async Task EditPhoto(AmblOnGraph amblGraph, string username, string entApiKey, UserPhoto photo, Guid albumID)
        {
            ensureStateObject();

            var existingAlbum = State.UserAlbums.FirstOrDefault(x => x.Photos.Any(y => y.ID == photo.ID));

            if (existingAlbum != null)
            {
                var existingPhoto = existingAlbum.Photos.FirstOrDefault(x => x.ID == photo.ID);

                if (existingPhoto != null)
                {
                    var photoResp = await amblGraph.EditPhoto(username, entApiKey, photo, albumID);

                    if (photoResp.Status)
                    {
                        existingAlbum.Photos.Remove(existingPhoto);

                        existingAlbum.Photos.Add(photo);

                        existingAlbum.Photos = existingAlbum.Photos.Distinct().ToList();
                    }
                }
            }

            State.Loading = false;
        }

        public virtual async Task EditTopList(AmblOnGraph amblGraph, string username, string entApiKey, UserTopList topList)
        {
            ensureStateObject();

            var existing = State.UserTopLists.FirstOrDefault(x => x.ID == topList.ID);

            if (existing != null)
            {
                var topListResp = await amblGraph.EditTopList(username, entApiKey, topList);

                if (topListResp.Status)
                {

                    State.UserTopLists.Remove(existing);

                    State.UserTopLists.Add(topList);

                    State.UserTopLists = State.UserTopLists.Distinct().ToList();
                }
            }

            State.Loading = false;
        }

        public virtual async Task EditUserInfo(AmblOnGraph amblGraph, string username, string entApiKey, UserInfo userInfo)
        {
            ensureStateObject();

            var existing = State.UserInfo;

            if (existing != null)
            {
                var userInfoResp = await amblGraph.EditUserInfo(username, entApiKey, userInfo);

                if (userInfoResp.Status)
                {
                    State.UserInfo = userInfo;
                }
            }
            else
                State.Error = "No User Info Record Exists";

            State.Loading = false;
        }

        public virtual async Task ItineraryItemOrderAdjusted(AmblOnGraph amblGraph, string email, string entApiKey, Itinerary itinerary)
        {

            var baseQuery = "g.V(\"" + itinerary.ID.ToString() + "\").Out(\"Contains\").coalesce(";

            var aGquery = "";

            var aQuery = "";

            itinerary.ActivityGroups.ForEach(
                (activitygroup) => {
                    aGquery = aGquery + "has(\"id\", \"" + activitygroup.ID.ToString() + "\").property(\"Order\", \"" + activitygroup.Order.ToString() + "\").property(\"Title\", \"" + activitygroup.Title.ToString() + "\"),";

                    activitygroup.Activities.ForEach(
                        (activity) => {
                            aQuery = aQuery + "has(\"id\", \"" + activity.ID.ToString() + "\").property(\"Order\", \"" + activity.Order.ToString() + "\"),";
                        });
                });

            var query = baseQuery + aGquery + ").out(\"Contains\").coalesce(" + aQuery + ")";

            var resp = await amblGraph.EditOrder(email, entApiKey, query);

            State.UserItineraries = await fetchUserItineraries(amblGraph, email, entApiKey);
        } 

        public virtual async Task QuickEditActivity(AmblOnGraph amblGraph, string username, string entApiKey, Activity activity)
        {
            var resp = await amblGraph.QuickEditActivity(activity);

            State.UserItineraries = await fetchUserItineraries(amblGraph, username, entApiKey);

            State.Loading = false;
        }
        
        #endregion
        public virtual async Task Refresh(AmblOnGraph amblGraph, AmblOnGraphFactory amblOnGraphFactory, string username, string entApiKey)
        {
            ensureStateObject();

            await Load(amblGraph, amblOnGraphFactory, username, entApiKey);

            // State.ExcludedCuratedLocations = await fetchUserExcludedCurations(amblGraph, username, entApiKey);

            // var userMap = State.UserMaps.FirstOrDefault(x => x.ID == State.SelectedUserMapID);

            // if (userMap != null)
            // {
            //     State.VisibleUserLocations = limitUserLocationsGeographically(State.AllUserLocations, userMap.Coordinates)
            //                                     .Distinct()
            //                                     .ToList();
            //     //State.VisibleUserLocations = State.VisibleUserLocations.Distinct().ToList();
            // }

            // var userLayer = State.UserLayers.Where(x => x.Title == "User").FirstOrDefault();

            // var userLayerID = (userLayer == null) ? Guid.Empty : userLayer.ID;
            
            
            // State.UserTopLists = await fetchUserTopLists(amblGraph, username, entApiKey, userLayerID);

            State.Loading = false;
        }

        // public virtual async Task GlobalSearch(string searchTerm)
        // {
        //     ensureStateObject();

        //     var userMap = State.UserMaps.FirstOrDefault(x => x.ID == State.SelectedUserMapID);

        //     if (userMap != null)
        //     {
        //         var circle = computeCircle(userMap.Coordinates[0], userMap.Coordinates[1], userMap.Coordinates[2], userMap.Coordinates[3]);

        //         var searchLocations = limitUserLocationsBySearch(State.AllUserLocations, searchTerm);

        //         var radiusLocations = limitUserLocationsByRadius(searchLocations, circle.Item1, circle.Item2, circle.Item3);

        //         State.LocalSearchUserLocations = radiusLocations
        //                 .Distinct()
        //                 .OrderBy(x => x.Title)
        //                 .ToList();

        //         var localIDs = State.LocalSearchUserLocations.Select(x => x.ID);

        //         State.OtherSearchUserLocations = searchLocations
        //                 .Where(x => !localIDs.Contains(x.ID))
        //                 .Distinct()
        //                 .OrderBy(x => x.Title)
        //                 .ToList();
        //     }

        //     State.Loading = false;
        // }

        public virtual async Task Load(AmblOnGraph amblGraph, AmblOnGraphFactory amblOnGraphFactory, string username, string entApiKey)
        {
            ensureStateObject();

            var userInfoResp = await amblGraph.GetUserInfo(username, entApiKey);

            if (userInfoResp.Status)
            {
                State.UserInfo = userInfoResp.Model;
                State.UserInfo.Email = username;
            }

            State.UserAlbums = await fetchUserAlbums(amblGraph, username, entApiKey);

            State.UserItineraries = await fetchUserItineraries(amblGraph, username, entApiKey);


            if(State.AllUserLocations.Count == 0){

                State.AllUserLocations = await amblGraph.PopulateAllLocations(username, entApiKey);
            };

            //var userLayer = State.UserLayers.Where(x => x.Title == "User").FirstOrDefault();

            //var userLayerID = (userLayer == null) ? Guid.Empty : userLayer.ID;

            //State.UserTopLists = await fetchUserTopLists(amblGraph, username, entApiKey);

            //State.ExcludedCuratedLocations = await fetchUserExcludedCurations(amblGraph, username, entApiKey);

            State.Loading = false;
        }

        // public virtual async Task LoadCuratedLocationsIntoDB(AmblOnGraph amblGraph, string ownerUsername, string entApiKey, List<dynamic> list, List<string> acclist, Guid layerID)
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
        //         var resp = await amblGraph.AddLocation(ownerUsername, entApiKey, location);

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
        //                     var accResp = await amblGraph.AddAccolade(ownerUsername, entApiKey, accolade, resp.Model);
        //                 }
        //             });
        //         }
        //     });
        // }

        public virtual async Task RemoveSelectedLayer(Guid layerID)
        {
            ensureStateObject();

            State.SelectedUserLayerIDs.Remove(layerID);

            State.VisibleUserLocations = removeUserLocationsByLayerID(State.VisibleUserLocations, layerID);

            State.Loading = false;
        }

        public virtual async Task SendInvites(ApplicationManagerClient appMgr, string entApiKey, List<string> usernames)
        {
            ensureStateObject();

            var subject = Environment.GetEnvironmentVariable("INVITE-EMAIL-SUBJECT");
            var message = Environment.GetEnvironmentVariable("INVITE-EMAIL").Replace("%%BASE-URL%%", Environment.GetEnvironmentVariable("BASE-URL"));
            var from = Environment.GetEnvironmentVariable("FROM-EMAIL");

            await usernames.Each(async (username) =>
            {
                var meta = new MetadataModel();

                var mail = new
                {
                    EmailFrom = from,
                    EmailTo = username,
                    Subject = subject,
                    Content = message
                };

                var obj = JToken.FromObject(mail);

                meta.Metadata["AccessRequestEmail"] = obj;

                try
                {
                    var resp = await appMgr.SendAccessRequestEmail(meta, entApiKey);
                }
                catch (Exception ex)
                {

                }
            });

            State.Loading = false;
        }

        // public virtual async Task SetSelectedMap(Guid mapID)
        // {
        //     ensureStateObject();

        //     var userMap = State.UserMaps.FirstOrDefault(x => x.ID == mapID);

        //     if (userMap != null)
        //     {
        //         State.SelectedUserMapID = mapID;

        //         // TODO: Filter results out a local collection of all locations 
        //         //var visibleLocations = await fetchVisibleUserLocations(username, entApiKey, State.SelectedUserLayerIDs);

        //         State.VisibleUserLocations = limitUserLocationsGeographically(State.AllUserLocations, userMap.Coordinates)
        //                                     .Distinct()
        //                                     .ToList();

        //         //State.VisibleUserLocations = State.VisibleUserLocations.Distinct().ToList();
        //     }

        //     State.Loading = false;
        // }

        public virtual async Task ShareItineraries(ApplicationManagerClient appMgr, AmblOnGraph amblGraph, string username, string entApiKey, 
            List<Itinerary> itineraries, List<string> usernames)
        {
            ensureStateObject();

            var name = username;

            if (State.UserInfo != null)
                name = State.UserInfo.FirstName + " " + State.UserInfo.LastName;

            var subject = Environment.GetEnvironmentVariable("SHARED-ITINERARY-EMAIL-SUBJECT").Replace("%%USER-NAME%%", name);
            var message = Environment.GetEnvironmentVariable("SHARED-ITINERARY-EMAIL").Replace("%%BASE-URL%%", Environment.GetEnvironmentVariable("BASE-URL"));
            var from = Environment.GetEnvironmentVariable("FROM-EMAIL");

            Dictionary<string, string> results = new Dictionary<string, string>();

            var success = true;

            await usernames.Each(async (user) =>
            {
                await itineraries.Each(async (itinerary) =>
                {

                    var result = await amblGraph.ShareItinerary(username, entApiKey, itinerary, user);

                    await addLocationFromSharedItinerary(amblGraph, user, entApiKey, itinerary);

                    State.SharedStatus = result.Status;

                    if (State.SharedStatus){
                        var mail = new
                        {
                            EmailTo = user,
                            EmailFrom = from,
                            Subject = subject,
                            Content = message
                        };

                        var meta = new MetadataModel();

                        meta.Metadata["AccessRequestEmail"] = JToken.FromObject(mail);

                        var resp = await appMgr.SendAccessRequestEmail(meta, entApiKey);

                        State.SharedStatus = resp.Status;
                    }
                });
            });

            State.Loading = false;
        }

        public virtual async Task UnshareItineraries(AmblOnGraph amblGraph, string entApiKey, List<Itinerary> itineraries, List<string> usernames)
        {
            ensureStateObject();

            var success = true;

            await usernames.Each(async (username) =>
            {
                await itineraries.Each(async (itinerary) =>
                {
                    var result = await amblGraph.UnshareItinerary(username, entApiKey, itinerary, username);

                    if (!result.Status)
                        success = false;
                });
            });

            if (!success)
                State.Error = "General Error unsharing itinerary.";

            State.Loading = false;
        }
        #endregion

        #region Helpers

        protected virtual async Task<List<Activity>> addLocationFromActivity(AmblOnGraph amblGraph, string email, string entAPIKey, List<ActivityLocationLookup> activityLocations)
        {
            var activities = new List<Activity>();

            foreach (ActivityLocationLookup acLoc in activityLocations){
                var location = await amblGraph.ensureLocation(email, entAPIKey, Guid.Empty, acLoc.Location);

                acLoc.Activity.LocationID = location.ID;
                
                activities.Add(acLoc.Activity);

                var existing = State.AllUserLocations.FirstOrDefault(x => x.ID == location.ID);

                if (existing == null){
                    State.AllUserLocations.Add(location);
                }                   
            }
            return activities;                              
        }

        protected virtual async Task<Status> addLocationFromSharedItinerary(AmblOnGraph amblGraph, string email, string entAPIKey, Itinerary itinerary)
        {
            await itinerary.ActivityGroups.Each(async (activityGroup) =>
            {
                await activityGroup.Activities.Each(async (activity) =>
                {   
                    if (activity.LocationID.HasValue){
                        var location = await amblGraph.ensureLocation(email, entAPIKey, activity.LocationID);

                        var existing = State.AllUserLocations.FirstOrDefault(x => x.ID == location.ID);

                        if (existing == null){
                            State.AllUserLocations.Add(location);
                        }     
                    }                      
                });
            });                    
            return Status.Success;                              
        }

        // Returns the radius and center of a circle inscribed within the bounded box
        protected virtual Tuple<float, float, float> computeCircle(float lat1, float long1, float lat2, float long2)
        {
            var coord1 = new GeoCoordinate(Convert.ToDouble(lat1), Convert.ToDouble(long1));
            var coord2 = new GeoCoordinate(Convert.ToDouble(lat2), Convert.ToDouble(long2));

            var distanceMeters = Math.Abs(coord1.GetDistanceTo(coord2));

            // Attach modulus to adjust for international date line       
            float aveLong = (long2 < long1) ? 180 + ((long1 + long2) / 2) : (long1 + long2) / 2;
            return new Tuple<float, float, float>(float.Parse((distanceMeters / 1609.344).ToString()), (lat1 + lat2) / 2, aveLong);
        }

        protected virtual void ensureStateObject()
        {
            State.Error = "";

            State.SharedStatus = null;

            //State.Status = "";

            if (State.SelectedUserLayerIDs == null)
                State.SelectedUserLayerIDs = new List<Guid>();

            if (State.UserLayers == null)
                State.UserLayers = new List<UserLayer>();

            if (State.UserMaps == null)
                State.UserMaps = new List<UserMap>();

            if (State.VisibleUserLocations == null)
                State.VisibleUserLocations = new List<UserLocation>();

            if (State.LocalSearchUserLocations == null)
                State.LocalSearchUserLocations = new List<UserLocation>();

            if (State.OtherSearchUserLocations == null)
                State.OtherSearchUserLocations = new List<UserLocation>();

            if (State.UserAlbums == null)
                State.UserAlbums = new List<UserAlbum>();

            if (State.UserItineraries == null)
                State.UserItineraries = new List<Itinerary>();

            State.UserTopLists = State.UserTopLists ?? new List<UserTopList>();

            State.AllUserLocations = State.AllUserLocations ?? new List<Location>();
        }

        // protected virtual async Task<List<UserAccolade>> fetchUserAccolades(AmblOnGraph amblGraph, string username, string entApiKey, Guid locationId)
        // {
        //     var userAccolades = new List<UserAccolade>();

        //     var accolades = await amblGraph.ListAccolades(username, entApiKey, locationId);

        //     accolades.Each((accolade) =>
        //     {
        //         userAccolades.Add(mapUserAccolade(accolade, locationId));
        //     });

        //     return userAccolades;
        // }

        protected virtual async Task<List<UserAlbum>> fetchUserAlbums(AmblOnGraph amblGraph, string username, string entApiKey)
        {
            var albums = await amblGraph.ListAlbums(username, entApiKey);

            // await albums.Each(async (album) =>
            // {
            //     var photos = await amblGraph.ListPhotos(username, entApiKey, album.ID);

            //     userAlbums.Add(mapUserAlbum(album, photos));
            // });

            return albums;
        }

        protected virtual async Task<List<Itinerary>> fetchUserItineraries(AmblOnGraph amblGraph, string username, string entApiKey)
        {
            var itineraries = await amblGraph.ListItineraries(username, entApiKey);

            //await itineraries.Each(async (itinerary) =>
            // await Each(itineraries, async (itinerary) =>
            // {
            //     //var amblGraph = amblGraphFactory.Create();

            //     itinerary.ActivityGroups = await amblGraph.ListActivityGroups(username, entApiKey, itinerary);

            //     //await itinerary.ActivityGroups.Each(async (activityGroup) =>
            //     await Each(itinerary.ActivityGroups, async (activityGroup) =>
            //     {
            //         //var amblGraph = amblGraphFactory.Create();

            //         activityGroup.Activities = await amblGraph.ListActivities(username, entApiKey, activityGroup.ID.Value);

            //         return false;

            //     }, parallel:false);
            //     return false;
            // }, parallel:false); 

            return itineraries;
        }

        public virtual async Task Each<T>(IEnumerable<T> values, Func<T, Task<bool>> action, bool parallel = false)
        {
            if (values != null)
            {
                if (parallel)
                {
                    var valueTasks = values.Select(value =>
                    {
                        return action(value);
                    });

 

                    var successful = await Task.WhenAll(valueTasks);

 

                    //Parallel.ForEach(values, value => action(value));  //  TODO:  Implement Multi-Threaded break logic
                }
                else
                {
                    bool shouldBreak;

 

                    foreach (T value in values)
                    {
                        shouldBreak = await action(value);

 

                        if (shouldBreak)
                            break;
                    }
                }
            }
        }
        // protected virtual async Task<List<UserLayer>> fetchUserLayers(AmblOnGraph amblGraph, string username, string entApiKey)
        // {
        //     var userLayers = new List<UserLayer>();

        //     var layers = await amblGraph.ListLayers(username, entApiKey);

        //     layers.Each(
        //         (layer) =>
        //         {
        //             userLayers.Add(mapUserLayer(layer));
        //         });

        //     var sharedLayers = await amblGraph.ListSharedLayers(username, entApiKey);

        //     sharedLayers.Each(
        //         (layerInfo) =>
        //         {
        //             float[] coords = null;

        //             var associatedMap = State.UserMaps.FirstOrDefault(x => x.ID == layerInfo.Item1.DefaultMapID);

        //             if (associatedMap != null)
        //                 coords = associatedMap.Coordinates;

        //             // Insert curated layer first
        //             userLayers.Insert(0, mapUserLayer(layerInfo.Item1, layerInfo.Item2, coords));
        //         });

        //     return userLayers;
        // }

        protected virtual async Task<List<UserMap>> fetchUserMaps(AmblOnGraph amblGraph, string username, string entApiKey)
        {
            var userMaps = new List<UserMap>();

            var maps = await amblGraph.ListMaps(username, entApiKey);

            maps.Each(
                (map) =>
                {
                    userMaps.Add(mapUserMap(map));
                });

            var sharedMaps = await amblGraph.ListSharedMaps(username, entApiKey);

            sharedMaps.Each(
                (mapInfo) =>
                {
                    userMaps.Add(mapUserMap(mapInfo.Item1, mapInfo.Item2));
                });

            return userMaps;
        }

        // Need clarification on the behavior of this - is this supposed to pull back all locations across all layers? 
        protected virtual async Task<List<UserLocation>> fetchVisibleUserLocations(AmblOnGraph amblGraph, string username, string entApiKey, List<Guid> layerIDs)
        {
            var userLocations = new List<UserLocation>();

            // await layerIDs.Each(async (layerID) =>
            // {
                var userLayerLocations = new List<UserLocation>();

                // var layer = State.UserLayers.FirstOrDefault(x => x.ID == layerID);

                var locations = await amblGraph.ListLocations(username, entApiKey);

                await locations.Each(async (location) =>
                {
                    var loc = mapUserLocation(location);

                    //var accolades = await fetchUserAccolades(amblGraph, username, entApiKey, location.ID);
                    
                    //loc.Accolades = accolades;
                    
                    userLayerLocations.Add(loc);
                });

                // if (layer != null && layer.Coordinates != null)
                //     userLayerLocations = userLayerLocations.Where(x => x.Latitude <= layer.Coordinates[0]
                //                 && x.Latitude >= layer.Coordinates[2]
                //                 && x.Longitude <= layer.Coordinates[1]
                //                 && x.Longitude >= layer.Coordinates[3]).ToList();

                userLocations.AddRange(userLayerLocations);
            // });

            return userLocations;
        }

        protected virtual async Task<List<UserTopList>> fetchUserTopLists(AmblOnGraph amblGraph, string username, string entApiKey, Guid layerId)
        {
            var userTopLists = new List<UserTopList>();

            var topLists = await amblGraph.ListTopLists(username, entApiKey);

            await topLists.Each(async (topList) =>
            {
                var locations = await amblGraph.ListTopListLocations(username, entApiKey, topList.ID);

                userTopLists.Add(mapUserTopList(topList, locations, layerId));
            });

            return userTopLists;

        }

        protected virtual async Task<ExcludedCurations> fetchUserExcludedCurations(AmblOnGraph amblGraph, string username, string entApiKey)
        {
            var curations = await amblGraph.ListExcludedCurations(username, entApiKey);

            return curations;

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
                if (coordinates[1] <= coordinates[3])
                {

                    //Accounts for the possibility that the bottom left coordinate has a greater longitude value 
                    //than the top right, due to it being on the opposite side of the international date line
                    var result = userLocations.Where(x => x.Latitude <= coordinates[0]
                                                   && x.Latitude >= coordinates[2]
                                                   && x.Longitude <= 180.0
                                                   && x.Longitude >= coordinates[3])
                                    .Union(userLocations.Where(x => x.Latitude <= coordinates[0]
                                                    && x.Latitude >= coordinates[2]
                                                    && x.Longitude <= coordinates[1]
                                                    && x.Longitude >= -180.0)).ToList();
                    return result;
                }
                else
                {
                    var result = userLocations.Where(x => x.Latitude <= coordinates[0]
                                                   && x.Latitude >= coordinates[2]
                                                   && x.Longitude <= coordinates[1]
                                                   && x.Longitude >= coordinates[3]).ToList();
                    return result;
                }
            }
            else
                return userLocations;
        }

        protected virtual List<UserPhoto> mapImageDataToUserPhotos(List<UserPhoto> photos, List<ImageMessage> images)
        {
            //var photoCount = 0;

            photos.Each(
                (photo) =>
                {
                    var img = images?.FirstOrDefault(x => QueryHelpers.ParseQuery(x.Headers)["ID"].ToString() == photo.ID.ToString());

                    // if (img == null)
                    //     img = images[photoCount];

                    if (img != null)
                        photo.ImageData = img;

                    //photoCount++;
                });

            return photos;
        }
        // {
        //     // var photoCount = 0;

        //     images.Each(
        //         (image) =>{
        //             var imageID = QueryHelpers.ParseQuery(image.Headers)["ID"];
                    
        //             var photo = photos?.FirstOrDefault(x => x.ID.ToString() == imageID.ToString());

        //             if(photo != null)
        //                 photo.ImageData = image;                                          
        //         }
        //     );
            
        //     return photos;
        // }

        protected virtual UserAccolade mapUserAccolade(Accolade accolade, Guid locationId)
        {
            return new UserAccolade()
            {
                ID = accolade.ID,
                LocationID = locationId,
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

            photos.Each(
                (photo) =>
                {
                    var userPhoto = mapUserPhoto(photo);
                    userAlbum.Photos.Add(userPhoto);
                });

            return userAlbum;
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

        protected virtual UserLocation mapUserLocation(Location location)
        {
            return new UserLocation()
            {
                ID = location.ID,
                Address = location.Address,
                Country = location.Country,
                //Deletable = userOwns,
                GoogleLocationName = location.GoogleLocationName,
                Icon = location.Icon,
                Instagram = location.Instagram,
                Latitude = location.Latitude,
                //LayerID = layerID,
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
            var coords = map.Coordinates?.Split(",");
            var fCoords = new float[4];

            if (coords != null && coords.Count() == 4)
            {
                hasCoords = true;
                var count = 0;

                coords.ToList().Each(
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
            var coords = parent.Coordinates?.Split(",");
            var fCoords = new float[4];

            if (coords != null && coords.Count() == 4)
            {
                hasCoords = true;
                var count = 0;

                coords.ToList().Each(
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

            locations.Each(
                (location) =>
                {
                    var userLocation = mapUserLocation(location);
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
