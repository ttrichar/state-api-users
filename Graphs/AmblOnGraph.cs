using AmblOn.State.API.Users.Models;
using Fathym;
using Fathym.API;
using Fathym.Business.Models;
using Gremlin.Net.Process.Traversal;
using LCU.Graphs;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AmblOn.State.API.Users.Graphs
{
    public class AmblOnGraph : LCUGraph
    {
        #region Properties

        #endregion

        #region Constructors
        public AmblOnGraph(LCUGraphConfig config)
            : base(config)
        { }
        #endregion

        #region API Methods

        #region Add 
        public virtual async Task<BaseResponse<Guid>> AddAlbum(string email, string entAPIKey, UserAlbum album)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + album.Title.Replace(" ","");

                var existingAlbumQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.AlbumVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
                var existingAlbums = await Submit<Album>(existingAlbumQuery);

                var existingAlbum = existingAlbums?.FirstOrDefault();

                if (existingAlbum == null)
                {
                    var createQuery = g.AddV(AmblOnGraphConstants.AlbumVertexName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, entAPIKey.ToString())
                        .Property("Lookup", lookup)
                        .Property("Title", album.Title ?? "");

                    var createAlbumResults = await Submit<Album>(createQuery);

                    var createdAlbum = createAlbumResults?.FirstOrDefault();

                    var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdAlbum.ID));

                    await Submit(userEdgeQuery);

                    return new BaseResponse<Guid>()
                    {
                        Model = createdAlbum.ID,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { Status = Status.Conflict.Clone("An album with that title already exists for this user.")};
            });
        }

        public virtual async Task<BaseResponse<Guid>> AddItinerary(string email, string entAPIKey, UserItinerary itinerary)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + itinerary.Title.Replace(" ", "_") + "|" + itinerary.StartDate.ToString() + "|" + itinerary.EndDate.ToString();

                var existingItineraryQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
                var existingItineraries = await Submit<Itinerary>(existingItineraryQuery);

                var existingItinerary = existingItineraries?.FirstOrDefault();

                if (existingItinerary == null)
                {
                    var createQuery = g.AddV(AmblOnGraphConstants.ItineraryVertexName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, itinerary.StartDate.ToShortDateString() + itinerary.EndDate.ToShortDateString())
                        .Property("Lookup", lookup)
                        .Property("StartDate", itinerary.StartDate)
                        .Property("EndDate", itinerary.EndDate)
                        .Property("CreatedDateTime", itinerary.CreatedDateTime)
                        .Property("Title", itinerary.Title ?? "");

                    var createItineraryResults = await Submit<Itinerary>(createQuery);

                    var createdItinerary = createItineraryResults?.FirstOrDefault();

                    var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdItinerary.ID));

                    await Submit(userEdgeQuery);

                    return new BaseResponse<Guid>()
                    {
                        Model = createdItinerary.ID,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { Status = Status.Conflict.Clone("An itinerary with that title and start/end date already exists for this user.")};
            });
        }

        public virtual async Task<BaseResponse<Guid>> AddItineraryActivity(string email, string entAPIKey, UserItineraryActivity itineraryActivity, Guid itineraryID, Guid locationID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + itineraryID.ToString() + "|" + itineraryActivity.StartDateTime.ToString() + "|" + itineraryActivity.EndDateTime.ToString() + "|" + itineraryActivity.LocationID.ToString();

                var existingItineraryActivityQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itineraryID)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryActivityVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
                var existingItineraryActivities = await Submit<ItineraryActivity>(existingItineraryActivityQuery);

                var existingItineraryActivity = existingItineraryActivities?.FirstOrDefault();

                if (existingItineraryActivity == null)
                {
                    var createQuery = g.AddV(AmblOnGraphConstants.ItineraryActivityVertexName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, itineraryActivity.StartDateTime.ToShortDateString() + itineraryActivity.EndDateTime.ToShortDateString())
                        .Property("Lookup", lookup)
                        .Property("StartDateTime", itineraryActivity.StartDateTime)
                        .Property("EndDateTime", itineraryActivity.EndDateTime)
                        .Property("CreatedDateTime", itineraryActivity.CreatedDateTime)
                        .Property("ActivityName", itineraryActivity.ActivityName ?? "");

                    var createItineraryActivityResults = await Submit<ItineraryActivity>(createQuery);

                    var createdItineraryActivity = createItineraryActivityResults?.FirstOrDefault();

                    var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdItineraryActivity.ID));

                    await Submit(userEdgeQuery);
                    
                    var itineraryEdgeQuery = g.V(itineraryID).AddE(AmblOnGraphConstants.ContainsEdgeName).To(g.V(createdItineraryActivity.ID));

                    await Submit(itineraryEdgeQuery);

                    var locationEdgeQuery = g.V(createdItineraryActivity.ID).AddE(AmblOnGraphConstants.OccursAtEdgeName).To(g.V(locationID));

                    await Submit(locationEdgeQuery);

                    return new BaseResponse<Guid>()
                    {
                        Model = createdItineraryActivity.ID,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { Status = Status.Conflict.Clone("An itinerary activity for that user's itinerary exists at the same date/time/location.")};
            });
        }

        public virtual async Task<BaseResponse<Guid>> AddLayer(string email, string entAPIKey, UserLayer layer)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + layer.Title.Replace(" ","");
                
                var existingLayerQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.LayerVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
                var existingLayers = await Submit<Layer>(existingLayerQuery);

                var existingLayer = existingLayers?.FirstOrDefault();

                if (existingLayer == null)
                {
                    var createQuery = g.AddV(AmblOnGraphConstants.LayerVertexName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, entAPIKey.ToString())
                        .Property("Lookup", lookup)
                        .Property("Title", layer.Title);

                    var createLayerResults = await Submit<Layer>(createQuery);

                    var createdLayer = createLayerResults?.FirstOrDefault();

                    var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdLayer.ID));

                    await Submit(userEdgeQuery);

                    return new BaseResponse<Guid>()
                    {
                        Model = createdLayer.ID,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { Status = Status.Conflict.Clone("A layer by that name already exists in for this user.")};
            });
        }

        public virtual async Task<BaseResponse<Guid>> AddLocation(string email, string entAPIKey, UserLocation location)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = location.LayerID.ToString() + "|" + location.Latitude.ToString() + "|" + location.Longitude.ToString();

                var existingLocationQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.LayerVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, location.LayerID)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.LocationVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
                var existingLocations = await Submit<Location>(existingLocationQuery);

                var existingLocation = existingLocations?.FirstOrDefault();

                if (existingLocation == null)
                {
                    var createQuery = g.AddV(AmblOnGraphConstants.LocationVertexName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, Convert.ToInt32(location.Latitude).ToString() + Convert.ToInt32(location.Longitude).ToString())
                        .Property("Lookup", lookup)
                        .Property("Address", location.Address ?? "")
                        .Property("Country", location.Country ?? "")
                        .Property("GoogleLocationName", location.GoogleLocationName ?? "")
                        .Property("Icon", location.Icon ?? "")
                        .Property("Instagram", location.Instagram ?? "")
                        .Property("Latitude", location.Latitude)
                        .Property("Longitude", location.Longitude)
                        .Property("State", location.State ?? "")
                        .Property("Telephone", location.Telephone ?? "")
                        .Property("Title", location.Title ?? "")
                        .Property("Town", location.Town ?? "")
                        .Property("Website", location.Website ?? "")
                        .Property("ZipCode", location.ZipCode ?? "");

                    var createLocationResults = await Submit<Location>(createQuery);

                    var createdLocation = createLocationResults?.FirstOrDefault();

                    var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdLocation.ID));

                    await Submit(userEdgeQuery);

                    var layerEdgeQuery = g.V(location.LayerID).AddE(AmblOnGraphConstants.ContainsEdgeName).To(g.V(createdLocation.ID));

                    await Submit(layerEdgeQuery);

                    return new BaseResponse<Guid>()
                    {
                        Model = createdLocation.ID,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { Status = Status.Conflict.Clone("A location by that lat/long already exists in selected layer.")};
            });
        }

        public virtual async Task<BaseResponse<Guid>> AddMap(string email, string entAPIKey, UserMap map)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + map.Title.Replace(" ","");

                var existingMapQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.MapVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
                var existingMaps = await Submit<Map>(existingMapQuery);

                var existingMap = existingMaps?.FirstOrDefault();

                if (existingMap == null)
                {
                    var createQuery = g.AddV(AmblOnGraphConstants.MapVertexName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, entAPIKey.ToString())
                        .Property("Lookup", lookup)
                        .Property("Title", map.Title)
                        .Property("Zoom", map.Zoom)
                        .Property("Latitude", map.Latitude)
                        .Property("Longitude", map.Longitude)
                        .Property("Primary", true)
                        .Property("Coordinates", String.Join(",", map.Coordinates))
                        .Property("DefaultLayerID", map.DefaultLayerID);

                    var createMapResults = await Submit<Map>(createQuery);

                    var createdMap = createMapResults?.FirstOrDefault();

                    var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdMap.ID));

                    await Submit(userEdgeQuery);

                    await setPrimaryMap(email, entAPIKey, createdMap.ID);

                    return new BaseResponse<Guid>()
                    {
                        Model = createdMap.ID,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { Status = Status.Conflict.Clone("A map by that name already exists for this user.")};
            });
        }

        public virtual async Task<BaseResponse<Guid>> AddPhoto(string email, string entAPIKey, UserPhoto photo, Guid albumID, Guid locationID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + albumID.ToString() + "|" + photo.URL + "|" + photo.LocationID.ToString();

                var existingPhotoQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.AlbumVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, albumID)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.PhotoVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
                var existingPhotos = await Submit<Photo>(existingPhotoQuery);

                var existingPhoto = existingPhotos?.FirstOrDefault();

                if (existingPhoto == null)
                {
                    var createQuery = g.AddV(AmblOnGraphConstants.PhotoVertexName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, entAPIKey.ToString())
                        .Property("Lookup", lookup)
                        .Property("Caption", photo.Caption ?? "")
                        .Property("URL", photo.URL ?? "");

                    var createPhotoResults = await Submit<Photo>(createQuery);

                    var createdPhoto = createPhotoResults?.FirstOrDefault();

                    var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdPhoto.ID));

                    await Submit(userEdgeQuery);
                    
                    var albumEdgeQuery = g.V(albumID).AddE(AmblOnGraphConstants.ContainsEdgeName).To(g.V(createdPhoto.ID));

                    await Submit(albumEdgeQuery);

                    var locationEdgeQuery = g.V(createdPhoto.ID).AddE(AmblOnGraphConstants.OccursAtEdgeName).To(g.V(locationID));

                    await Submit(locationEdgeQuery);

                    return new BaseResponse<Guid>()
                    {
                        Model = createdPhoto.ID,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { Status = Status.Conflict.Clone("A photo for that user's album exists with the same URL.")};
            });
        }
        
        public virtual async Task<BaseResponse<Guid>> AddSharedLayer(string email, string entAPIKey, UserLayer layer, bool deletable, Guid parentID, Guid defaultMapID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + layer.Title.Replace(" ","");
                
                var existingLayerQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.SharedLayerVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
                var existingLayers = await Submit<SharedLayer>(existingLayerQuery);

                var existingLayer = existingLayers?.FirstOrDefault();

                if (existingLayer == null)
                {
                    var createQuery = g.AddV(AmblOnGraphConstants.SharedLayerVertexName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, entAPIKey.ToString())
                        .Property("Lookup", lookup)
                        .Property("Title", layer.Title)
                        .Property("DefaultMapID", defaultMapID)
                        .Property("Deletable", deletable);

                    var createLayerResults = await Submit<SharedLayer>(createQuery);

                    var createdLayer = createLayerResults?.FirstOrDefault();

                    var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdLayer.ID));

                    await Submit(userEdgeQuery);

                    var parentEdgeQuery = g.V(createdLayer.ID).AddE(AmblOnGraphConstants.InheritsEdgeName).To(g.V(parentID));

                    await Submit(parentEdgeQuery);

                    return new BaseResponse<Guid>()
                    {
                        Model = createdLayer.ID,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { Status = Status.Conflict.Clone("A layer by that name already exists in for this user.")};
            });
        }

        public virtual async Task<BaseResponse<Guid>> AddSharedMap(string email, string entAPIKey, UserMap map, Guid parentID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + map.Title.Replace(" ","");

                var existingMapQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.SharedMapVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
                var existingMaps = await Submit<SharedMap>(existingMapQuery);

                var existingMap = existingMaps?.FirstOrDefault();

                if (existingMap == null)
                {
                    var createQuery = g.AddV(AmblOnGraphConstants.SharedMapVertexName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, entAPIKey.ToString())
                        .Property("Lookup", lookup)
                        .Property("Title", map.Title)
                        .Property("Deletable", true)
                        .Property("Primary", true)
                        .Property("DefaultLayerID", map.DefaultLayerID);

                    var createMapResults = await Submit<SharedMap>(createQuery);

                    var createdMap = createMapResults?.FirstOrDefault();

                    var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdMap.ID));

                    await Submit(userEdgeQuery);

                    var parentEdgeQuery = g.V(createdMap.ID).AddE(AmblOnGraphConstants.InheritsEdgeName).To(g.V(parentID));

                    await Submit(parentEdgeQuery);

                    await setPrimaryMap(email, entAPIKey, createdMap.ID);

                    return new BaseResponse<Guid>()
                    {
                        Model = createdMap.ID,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { Status = Status.Conflict.Clone("A map by that name already exists for this user.")};
            });
        }

        public virtual async Task<BaseResponse<Guid>> AddSharedMap(string email, string entAPIKey, SharedMap map, bool deletable, Guid parentID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + map.Title.Replace(" ","");

                var existingMapQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.SharedMapVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
                var existingMaps = await Submit<SharedMap>(existingMapQuery);

                var existingMap = existingMaps?.FirstOrDefault();

                if (existingMap == null)
                {
                    var createQuery = g.AddV(AmblOnGraphConstants.SharedMapVertexName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, entAPIKey.ToString())
                        .Property("Lookup", lookup)
                        .Property("Primary", true)
                        .Property("Title", map.Title)
                        .Property("Deletable", deletable)
                        .Property("DefaultLayerID", map.DefaultLayerID);

                    var createMapResults = await Submit<SharedMap>(createQuery);

                    var createdMap = createMapResults?.FirstOrDefault();

                    var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdMap.ID));

                    await Submit(userEdgeQuery);

                    var parentEdgeQuery = g.V(createdMap.ID).AddE(AmblOnGraphConstants.InheritsEdgeName).To(g.V(parentID));

                    await Submit(parentEdgeQuery);

                    await setPrimaryMap(email, entAPIKey, createdMap.ID);

                    return new BaseResponse<Guid>()
                    {
                        Model = createdMap.ID,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { Status = Status.Conflict.Clone("A map by that name already exists for this user.")};
            });
        }

        public virtual async Task<BaseResponse<Guid>> AddTopList(string email, string entAPIKey, UserTopList topList)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + topList.Title.Replace(" ","");

                var existingTopListQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.TopListVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
                var existingTopLists = await Submit<TopList>(existingTopListQuery);

                var existingTopList = existingTopLists?.FirstOrDefault();

                if (existingTopList == null)
                {
                    // Add the Top List
                    var createQuery = g.AddV(AmblOnGraphConstants.TopListVertexName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, entAPIKey.ToString())
                        .Property("Lookup", lookup)
                        .Property("Title", topList.Title ?? "")
                        .Property("OrderedValue", topList.OrderedValue);
                        //TODO: Add map marker nodes

                    var createTopList = await Submit<TopList>(createQuery);

                    var createdTopList = createTopList?.FirstOrDefault();

                    // Add edge to from user vertex to newly created top list vertex
                    var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdTopList.ID));
                    await Submit(userEdgeQuery);

                    // Add edges to each location - locations are presumed to already exist in the graph
                    foreach (UserLocation loc in topList.LocationList) {
                        var locationQuery = g.V(createdTopList.ID).AddE(AmblOnGraphConstants.ContainsEdgeName).To(g.V(loc.ID));
                        await Submit(locationQuery);
                    }

                    return new BaseResponse<Guid>()
                    {
                        Model = createdTopList.ID,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { Status = Status.Conflict.Clone("An top list with that title already exists for this user.")};
            });
        }   
        #endregion
        
        #region Delete
        public virtual async Task<BaseResponse> DeleteAlbum(string email, string entAPIKey, Guid albumID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var existingAlbumQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.AlbumVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, albumID);
                
                var existingAlbums = await Submit<Album>(existingAlbumQuery);

                var existingAlbum = existingAlbums?.FirstOrDefault();

                if (existingAlbum != null)
                {
                    var deletePhotosQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.AlbumVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, albumID)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.PhotoVertexName)
                    .Drop();

                    await Submit(deletePhotosQuery);

                    var deleteQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.AlbumVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, albumID)
                    .Drop();

                    await Submit(deleteQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This album does not exist for this user")};
            });
        }

        public virtual async Task<BaseResponse> DeleteItinerary(string email, string entAPIKey, Guid itineraryID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var existingItineraryQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itineraryID);
                
                var existingItineraries = await Submit<Itinerary>(existingItineraryQuery);

                var existingItinerary = existingItineraries?.FirstOrDefault();

                if (existingItinerary != null)
                {
                    var deleteActivitiesQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itineraryID)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryActivityVertexName)
                    .Drop();

                    await Submit(deleteActivitiesQuery);

                    var deleteQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itineraryID)
                    .Drop();

                    await Submit(deleteQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This itinerary does not exist for this user")};
            });
        }

        public virtual async Task<BaseResponse> DeleteItineraryActivity(string email, string entAPIKey, Guid itineraryActivityID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var existingItineraryActivityQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryActivityVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itineraryActivityID);
                
                var existingItineraryActivities = await Submit<ItineraryActivity>(existingItineraryActivityQuery);

                var existingItineraryActivity = existingItineraryActivities?.FirstOrDefault();

                if (existingItineraryActivity != null)
                {
                    var deleteQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryActivityVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itineraryActivityID)
                    .Drop();

                    await Submit(deleteQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This itinerary activity does not exist for the user")};
            });
        }

        public virtual async Task<BaseResponse> DeleteLocation(string email, string entAPIKey, Guid locationID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var existingLocationQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.LocationVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, locationID);
                
                var existingLocations = await Submit<Location>(existingLocationQuery);

                var existingLocation = existingLocations?.FirstOrDefault();

                if (existingLocation != null)
                {
                    var deleteQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.LocationVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, locationID)
                    .Drop();

                    await Submit(deleteQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This location does not exist in the user's layer")};
            });
        }

        public virtual async Task<BaseResponse> DeleteMap(string email, string entAPIKey, Guid mapID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var existingMapQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.MapVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, mapID);
                
                var existingMaps = await Submit<Map>(existingMapQuery);

                var existingMap = existingMaps?.FirstOrDefault();

                if (existingMap != null)
                {
                    var deleteQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.MapVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, mapID)
                    .Drop();

                    await Submit(deleteQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This map does not exist for this user")};
            });
        }

        public virtual async Task<BaseResponse> DeletePhoto(string email, string entAPIKey, Guid photoID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var existingPhotoQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.PhotoVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, photoID);
                
                var existingPhotos = await Submit<Photo>(existingPhotoQuery);

                var existingPhoto = existingPhotos?.FirstOrDefault();

                if (existingPhoto != null)
                {
                    var deleteQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.PhotoVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, photoID)
                    .Drop();

                    await Submit(deleteQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This photo does not exist for the user")};
            });
        }
        
        public virtual async Task<BaseResponse> DeleteSharedMap(string email, string entAPIKey, Guid mapID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var existingMapQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.SharedMapVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, mapID);
                
                var existingMaps = await Submit<SharedMap>(existingMapQuery);

                var existingMap = existingMaps?.FirstOrDefault();

                if (existingMap != null)
                {
                    var deleteQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.SharedMapVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, mapID)
                    .Drop();

                    await Submit(deleteQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This shared map does not exist for this user")};
            });
        }

        public virtual async Task<BaseResponse> DeleteTopList(string email, string entAPIKey, Guid topListID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var existingTopListQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.TopListVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, topListID);
                
                var existingTopLists = await Submit<Album>(existingTopListQuery);

                var existingTopList = existingTopLists?.FirstOrDefault();

                if (existingTopList != null)
                {
                    var deleteTopListQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.TopListVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, topListID)
                    .Drop();

                    await Submit(deleteTopListQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This top list does not exist for this user")};
            });
        }
        
        public virtual async Task<BaseResponse> DeleteMaps(string email, string entAPIKey, Guid[] mapIDs)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);
               
                var existingMapsQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.MapVertexName)
                    .Has("ID",  P.Inside(mapIDs));
                
                var existingMaps = await Submit<Map>(existingMapsQuery);

                if (existingMaps != null)
                {
                    var deleteQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.MapVertexName)
                    .Has("ID",  P.Inside(mapIDs))
                    .Drop();

                    await Submit(deleteQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This map does not exist for this user")};
            });
        }

        #endregion
        
        #region Edit
        public virtual async Task<BaseResponse> EditAlbum(string email, string entAPIKey, UserAlbum album)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + album.Title.Replace(" ","");

                var existingAlbumQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.AlbumVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, album.ID);
                
                var existingAlbums = await Submit<Album>(existingAlbumQuery);

                var existingAlbum = existingAlbums?.FirstOrDefault();

                if (existingAlbum != null)
                {
                    var editQuery = g.V(album.ID)
                        .Property("Lookup", lookup)
                        .Property("Title", album.Title ?? "");

                    await Submit(editQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This album does not exist for this user")};
            });
        }

        public virtual async Task<BaseResponse> EditItinerary(string email, string entAPIKey, UserItinerary itinerary)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + itinerary.Title.Replace(" ", "_") + "|" + itinerary.StartDate.ToString() + "|" + itinerary.EndDate.ToString();

                var existingItineraryQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itinerary.ID);
                
                var existingItineraries = await Submit<Itinerary>(existingItineraryQuery);

                var existingItinerary = existingItineraries?.FirstOrDefault();

                if (existingItinerary != null)
                {
                    var editQuery = g.V(itinerary.ID)
                        .Property("Lookup", lookup)
                        .Property("StartDate", itinerary.StartDate)
                        .Property("EndDate", itinerary.EndDate)
                        .Property("Title", itinerary.Title ?? "");

                    await Submit(editQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This itinerary does not exist for this user")};
            });
        }

        public virtual async Task<BaseResponse> EditItineraryActivity(string email, string entAPIKey, UserItineraryActivity itineraryActivity, Guid itineraryID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + itineraryID.ToString() + "|" + itineraryActivity.StartDateTime.ToString() + "|" + itineraryActivity.EndDateTime.ToString() + "|" + itineraryActivity.LocationID.ToString();

                var existingItineraryActivityQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryActivityVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itineraryActivity.ID);
                
                var existingItineraryActivities = await Submit<ItineraryActivity>(existingItineraryActivityQuery);

                var existingItineraryActivity = existingItineraryActivities?.FirstOrDefault();

                if (existingItineraryActivity != null)
                {
                    var editQuery = g.V(itineraryActivity.ID)
                        .Property("Lookup", lookup)
                        .Property("StartDateTime", itineraryActivity.StartDateTime)
                        .Property("EndDateTime", itineraryActivity.EndDateTime)
                        .Property("ActivityName", itineraryActivity.ActivityName ?? "");

                    await Submit(editQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This itinerary activity does not exist for this user")};
            });
        }

        public virtual async Task<BaseResponse> EditLocation(string email, string entAPIKey, UserLocation location)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = location.LayerID.ToString() + "|" + location.Latitude.ToString() + "|" + location.Longitude.ToString();

                var existingLocationQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.LayerVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, location.LayerID)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.LocationVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, location.ID);
                
                var existingLocations = await Submit<Location>(existingLocationQuery);

                var existingLocation = existingLocations?.FirstOrDefault();

                if (existingLocation != null)
                {
                    var editQuery = g.V(location.ID)
                        .Property("Lookup", lookup)
                        .Property("Address", location.Address ?? "")
                        .Property("Country", location.Country ?? "")
                        .Property("Icon", location.Icon ?? "")
                        .Property("Instagram", location.Instagram ?? "")
                        .Property("Latitude", location.Latitude)
                        .Property("Longitude", location.Longitude)
                        .Property("State", location.State ?? "")
                        .Property("Telephone", location.Telephone ?? "")
                        .Property("Title", location.Title ?? "")
                        .Property("Town", location.Town ?? "")
                        .Property("Website", location.Website ?? "")
                        .Property("ZipCode", location.ZipCode ?? "");

                    await Submit(editQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This location does not exist in the user's layer")};
            });
        }

        public virtual async Task<BaseResponse> EditMap(string email, string entAPIKey, UserMap map)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + map.Title.Replace(" ","");

                var existingMapQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.MapVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, map.ID);
                
                var existingMaps = await Submit<Map>(existingMapQuery);

                var existingMap = existingMaps?.FirstOrDefault();

                if (existingMap != null)
                {
                    var editQuery = g.V(map.ID)
                        .Property(AmblOnGraphConstants.PartitionKeyName, entAPIKey.ToString())
                        .Property("Lookup", lookup)
                        .Property("Title", map.Title)
                        .Property("Zoom", map.Zoom)
                        .Property("Latitude", map.Latitude)
                        .Property("Longitude", map.Longitude)
                        .Property("Primary", map.Primary)
                        .Property("Coordinates", String.Join(",", map.Coordinates))
                        .Property("DefaultLayerID", map.DefaultLayerID);

                    await Submit(editQuery);

                    if (map.Primary)
                        await setPrimaryMap(email, entAPIKey, (map.ID.HasValue ? map.ID.Value : Guid.Empty));

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This map does not exist for this user")};
            });
        }

        public virtual async Task<BaseResponse> EditPhoto(string email, string entAPIKey, UserPhoto photo, Guid albumID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + albumID.ToString() + "|" + photo.URL + "|" + photo.LocationID.ToString();

                var existingPhotoQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.PhotoVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, photo.ID);
                
                var existingPhotos = await Submit<Photo>(existingPhotoQuery);

                var existingPhoto = existingPhotos?.FirstOrDefault();

                if (existingPhoto != null)
                {
                    var editQuery = g.V(photo.ID)
                        .Property("Lookup", lookup)
                        .Property("Caption", photo.Caption)
                        .Property("URL", photo.URL);

                    await Submit(editQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This photo does not exist for this user")};
            });
        }

        public virtual async Task<BaseResponse> EditSharedMap(string email, string entAPIKey, UserMap map)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + map.Title.Replace(" ","");

                var existingMapQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.SharedMapVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, map.ID);
                
                var existingMaps = await Submit<SharedMap>(existingMapQuery);

                var existingMap = existingMaps?.FirstOrDefault();

                if (existingMap != null)
                {
                    var editQuery = g.V(map.ID)
                        .Property(AmblOnGraphConstants.PartitionKeyName, entAPIKey.ToString())
                        .Property("Lookup", lookup)
                        .Property("Title", map.Title)
                        .Property("Deletable", true)
                        .Property("Primary", map.Primary)
                        .Property("DefaultLayerID", map.DefaultLayerID);

                    await Submit(editQuery);

                    if (map.Primary)
                        await setPrimaryMap(email, entAPIKey, (map.ID.HasValue ? map.ID.Value : Guid.Empty));

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This shared map does not exist for this user")};
            });
        }

        public virtual async Task<BaseResponse> EditTopList(string email, string entAPIKey, UserTopList topList)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + topList.Title.Replace(" ","");

                var existingTopListQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.TopListVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, topList.ID);
                
                var existingTopLists = await Submit<TopList>(existingTopListQuery);

                var existingTopList = existingTopLists?.FirstOrDefault();

                if (existingTopList != null)
                {
                    var editQuery = g.V(topList.ID)
                        .Property("Lookup", lookup)
                        .Property("Title", topList.Title ?? "");

                    await Submit(editQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This top list does not exist for this user")};
            });
        }
        #endregion 

        #region List
        public virtual async Task<List<Album>> ListAlbums(string email, string entAPIKey)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var query = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.AlbumVertexName);

                var results = await Submit<Album>(query);

                return results.ToList();
            });
        }

        public virtual async Task<List<Itinerary>> ListItineraries(string email, string entAPIKey)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var query = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName);

                var results = await Submit<Itinerary>(query);

                return results.ToList();
            });
        }

        public virtual async Task<List<ItineraryActivity>> ListItineraryActivities(string email, string entAPIKey, Guid itineraryID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var query = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itineraryID)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryActivityVertexName);

                var results = await Submit<ItineraryActivity>(query);

                results.ToList().ForEach(
                    (itineraryActivity) =>
                    {
                        var locationId = getItineraryActivityLocationID(userId, itineraryActivity.ID).GetAwaiter().GetResult();
                        itineraryActivity.LocationID = locationId;
                    });

                return results.ToList();
            });
        }

        public virtual async Task<List<Layer>> ListLayers(string email, string entAPIKey)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var query = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.LayerVertexName);

                var results = await Submit<Layer>(query);

                return results.ToList();
            });
        }

        public virtual async Task<List<Location>> ListLocations(string email, string entAPIKey, Guid layerID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var query = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.LayerVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, layerID)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.LocationVertexName);

                var results = await Submit<Location>(query);

                if (results.ToList().Count == 0)
                {
                    query = g.V(userId)
                        .Out(AmblOnGraphConstants.OwnsEdgeName)
                        .HasLabel(AmblOnGraphConstants.SharedLayerVertexName)
                        .Has(AmblOnGraphConstants.IDPropertyName, layerID)
                        .Out(AmblOnGraphConstants.InheritsEdgeName)
                        .HasLabel(AmblOnGraphConstants.LayerVertexName)
                        .Out(AmblOnGraphConstants.ContainsEdgeName)
                        .HasLabel(AmblOnGraphConstants.LocationVertexName);

                    results = await Submit<Location>(query);
                }

                return results.ToList();
            });
        }

        public virtual async Task<List<Map>> ListMaps(string email, string entAPIKey)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var query = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.MapVertexName);

                var results = await Submit<Map>(query);

                return results.ToList();
            });
        }

        public virtual async Task<List<Photo>> ListPhotos(string email, string entAPIKey, Guid albumID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var query = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.AlbumVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, albumID)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.PhotoVertexName);

                var results = await Submit<Photo>(query);

                results.ToList().ForEach(
                    (photo) =>
                    {
                        var locationId = getPhotoLocationID(userId, photo.ID).GetAwaiter().GetResult();
                        photo.LocationID = locationId;
                    });

                return results.ToList();
            });
        }

        public virtual async Task<List<Tuple<SharedLayer, Layer>>> ListSharedLayers(string email, string entAPIKey)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var query = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.SharedLayerVertexName);

                var results = await Submit<SharedLayer>(query);

                var returnValues = new List<Tuple<SharedLayer, Layer>>();

                results.ToList().ForEach(
                    (sharedLayer) =>
                    {
                        var layerQuery = g.V(userId)
                            .Out(AmblOnGraphConstants.OwnsEdgeName)
                            .HasLabel(AmblOnGraphConstants.SharedLayerVertexName)
                            .Has(AmblOnGraphConstants.IDPropertyName, sharedLayer.ID)
                            .Out(AmblOnGraphConstants.InheritsEdgeName)
                            .HasLabel(AmblOnGraphConstants.LayerVertexName);
                            
                        var layerResults = Submit<Layer>(layerQuery).GetAwaiter().GetResult();

                        var layer = layerResults.FirstOrDefault();

                        if (layer != null)
                            returnValues.Add(Tuple.Create<SharedLayer, Layer>(sharedLayer, layer));
                    });

                return returnValues;
            });
        }

        public virtual async Task<List<Tuple<SharedMap, Map>>> ListSharedMaps(string email, string entAPIKey)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var query = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.SharedMapVertexName);

                var results = await Submit<SharedMap>(query);

                var returnValues = new List<Tuple<SharedMap, Map>>();

                results.ToList().ForEach(
                    (sharedMap) =>
                    {
                        var mapQuery = g.V(userId)
                            .Out(AmblOnGraphConstants.OwnsEdgeName)
                            .HasLabel(AmblOnGraphConstants.SharedMapVertexName)
                            .Has(AmblOnGraphConstants.IDPropertyName, sharedMap.ID)
                            .Out(AmblOnGraphConstants.InheritsEdgeName)
                            .HasLabel(AmblOnGraphConstants.MapVertexName);
                            
                        var mapResults = Submit<Map>(mapQuery).GetAwaiter().GetResult();

                        var map = mapResults.FirstOrDefault();

                        if (map != null)
                            returnValues.Add(Tuple.Create<SharedMap, Map>(sharedMap, map));
                    });

                return returnValues;
            });
        }
        
        public virtual async Task<List<TopList>> ListTopLists(string email, string entAPIKey)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var query = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.TopListVertexName);

                var results = await Submit<TopList>(query);

                return results.ToList();
            });
        }

        #endregion
        
        #endregion

        #region Helpers
        public virtual async Task<Guid> ensureAmblOnUser(GraphTraversalSource g, string email, string entAPIKey)
        {
            var partKey = email?.Split('@')[1];

            var query = g.V().HasLabel(AmblOnGraphConstants.AmblOnUserVertexName)
                .Has(AmblOnGraphConstants.PartitionKeyName, partKey)
                .Has("Email", email);

            var results = await Submit<BusinessModel<Guid>>(query);

            var existingUser = results.Any() ? results.FirstOrDefault().ID : Guid.Empty;

            if (!results.Any())
            {
                existingUser = await setupNewUser(g, email, entAPIKey);
            }

            return existingUser;
        }

        public virtual async Task<Guid> getItineraryActivityLocationID(Guid userId, Guid itineraryActivityId)
        {
            return await withG(async (client, g) =>
            {
                var query = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryActivityVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itineraryActivityId)
                    .Out(AmblOnGraphConstants.OccursAtEdgeName)
                    .HasLabel(AmblOnGraphConstants.LocationVertexName);

                var results = await Submit<Location>(query);

                var location = results.FirstOrDefault();

                if (location != null)
                    return location.ID;
                else
                    return Guid.Empty;
            });
        }

        public virtual async Task<Guid> getPhotoLocationID(Guid userId, Guid photoId)
        {
            return await withG(async (client, g) =>
            {
                var query = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.PhotoVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, photoId)
                    .Out(AmblOnGraphConstants.OccursAtEdgeName)
                    .HasLabel(AmblOnGraphConstants.LocationVertexName);

                var results = await Submit<Location>(query);

                var location = results.FirstOrDefault();

                if (location != null)
                    return location.ID;
                else
                    return Guid.Empty;
            });
        }

        public virtual async Task<Status> setPrimaryMap(string email, string entAPIKey, Guid mapId)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var oldMapsQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.MapVertexName)
                    .Has("Primary", true)
                    .Property("Primary", false);

                await Submit(oldMapsQuery);

                var oldSharedMapsQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.SharedMapVertexName)
                    .Has("Primary", true)
                    .Property("Primary", false);

                await Submit(oldSharedMapsQuery);

                var existingMapQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.MapVertexName)
                    .Has("id", mapId);

                var existingMaps = await Submit<Map>(existingMapQuery);

                var existingMap = existingMaps?.FirstOrDefault();

                if (existingMap != null)
                {
                    existingMapQuery = g.V(userId)
                        .Out(AmblOnGraphConstants.OwnsEdgeName)
                        .HasLabel(AmblOnGraphConstants.MapVertexName)
                        .Has("id", mapId)
                        .Property("Primary", true);

                    await Submit(existingMapQuery);
                }
                else
                {
                    var existingSharedMapQuery =  g.V(userId)
                        .Out(AmblOnGraphConstants.OwnsEdgeName)
                        .HasLabel(AmblOnGraphConstants.SharedMapVertexName)
                        .Has("id", mapId);

                    var existingSharedMaps = await Submit<Map>(existingSharedMapQuery);

                    var existingSharedMap = existingSharedMaps?.FirstOrDefault();

                     if (existingSharedMap != null)
                    {
                        existingSharedMapQuery = g.V(userId)
                            .Out(AmblOnGraphConstants.OwnsEdgeName)
                            .HasLabel(AmblOnGraphConstants.SharedMapVertexName)
                            .Has("id", mapId)
                            .Property("Primary", true);

                        await Submit(existingSharedMapQuery);
                    }
                }

                return Status.Success;
            });
        }

        public virtual async Task<Guid> setupNewUser(GraphTraversalSource g, string email, string entAPIKey)
        {
            var partKey = email?.Split('@')[1];

            var query = g.AddV(AmblOnGraphConstants.AmblOnUserVertexName)
                    .Property(AmblOnGraphConstants.PartitionKeyName, partKey)
                    .Property("Email", email);

            var results = await Submit<BusinessModel<Guid>>(query);

            var user = results.Any() ? results.FirstOrDefault().ID : Guid.Empty;

            await AddLayer(email, entAPIKey, new UserLayer()
            {
                Title = "User Layer"
            });

            var sharedMapQuery = g.V()
                    .HasLabel(AmblOnGraphConstants.AmblOnUserVertexName)
                    .Has("Email", AmblOnGraphConstants.DefaultUserEmail)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.MapVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, AmblOnGraphConstants.DefaultUserID + "|DefaultMap");

            var sharedMapResults = await Submit<Map>(sharedMapQuery);

            var sharedMapResult = sharedMapResults.Any() ? sharedMapResults.FirstOrDefault().ID : Guid.Empty;

            var sharedLayerQuery = g.V()
                    .HasLabel(AmblOnGraphConstants.AmblOnUserVertexName)
                    .Has("Email", AmblOnGraphConstants.DefaultUserEmail)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.LayerVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, AmblOnGraphConstants.DefaultUserID + "|DefaultLayer");

            var sharedLayerResults = await Submit<Map>(sharedLayerQuery);

            var sharedLayerResult = sharedLayerResults.Any() ? sharedLayerResults.FirstOrDefault().ID : Guid.Empty;

            await AddSharedMap(email, entAPIKey, new SharedMap()
            {
                Title = "Global Map",
                Deletable = false,
                DefaultLayerID = sharedLayerResult
            }, false, sharedMapResult);

            await AddSharedLayer(email, entAPIKey, new UserLayer()
            {
                Title = "Curated Layer",
                Deletable = false,
            }, false, sharedLayerResult, sharedMapResult);

            return user;
        }
        #endregion
    }
}