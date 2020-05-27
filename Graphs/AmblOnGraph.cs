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
        public AmblOnGraph(GremlinClientPoolManager clientMgr)
            : base(clientMgr)
        { }
        #endregion

        #region API Methods 

        #region Add 
        public virtual async Task<BaseResponse<Guid>> AddAccolade(string email, string entAPIKey, UserAccolade accolade, Guid locationId)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = locationId.ToString() + "|" + accolade.Title.Replace(" ", "").Replace("'","");

                // Look up the accolade in the layer (curated layer, by default)
                var existingAccoladeQuery = g.V(locationId)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.AccoladeVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, lookup);

                var existingAccolades = await Submit<Accolade>(existingAccoladeQuery);

                var existingAccolade = existingAccolades?.FirstOrDefault();

                if (existingAccolade == null)
                {
                    // Add the accolade vertex
                    var createQuery = g.AddV(AmblOnGraphConstants.AccoladeVertexName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, entAPIKey.ToString())
                        .Property("Lookup", lookup)
                        .Property("Title", accolade.Title ?? "")
                        .Property("Year", accolade.Year ?? "")
                        .Property("Rank", accolade.Rank ?? "");

                    var createAccolade = await Submit<Accolade>(createQuery);

                    var createdAccolade = createAccolade?.FirstOrDefault();
                    createdAccolade.LocationID = locationId;

                    // Add edge from location vertex to newly created accolade vertex (Contains)
                    var locationEdgeQuery = g.V(locationId).AddE(AmblOnGraphConstants.ContainsEdgeName).To(g.V(createdAccolade.ID));
                    await Submit(locationEdgeQuery);

                    return new BaseResponse<Guid>()
                    {
                        Model = createdAccolade.ID,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { 
                        Model = existingAccolade.ID,
                        Status = Status.Conflict.Clone("An accolade with that title already exists for this layer.") 
                    };
            });
        }

        public virtual async Task<BaseResponse<Guid>> AddActivity(string email, string entAPIKey, Guid itineraryId, Guid activityGroupId, Activity activity)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + itineraryId.ToString() + "|" + activityGroupId.ToString() + "|" + activity.Title.Replace(" ", "_") + "|" + 
                (activity.LocationID.HasValue ? activity.LocationID.Value.ToString() : Guid.Empty.ToString()) + "|" + activity.Order.ToString();

                var existingActivityQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itineraryId)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ActivityGroupVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, activityGroupId)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ActivityVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
                var existingActivities = await Submit<Activity>(existingActivityQuery);

                var existingActivity = existingActivities?.FirstOrDefault();

                if (existingActivity == null)
                {
                    var createQuery = g.AddV(AmblOnGraphConstants.ActivityVertexName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, "Activity|" + activity.Title.Substring(0,1))
                        .Property("Lookup", lookup)
                        .Property("Checked", activity.Checked)
                        .Property("CreatedDateTime", activity.CreatedDateTime)
                        .Property("Favorited", activity.Favorited)
                        .Property("Notes", activity.Notes ?? "")
                        .Property("Order", activity.Order)
                        .Property("Title", activity.Title ?? "")
                        .Property("TransportIcon", activity.TransportIcon ?? "")
                        .Property("WidgetIcon", activity.WidgetIcon ?? "");

                    var createActivityResults = await Submit<Activity>(createQuery);

                    var createdActivity = createActivityResults?.FirstOrDefault();

                    var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdActivity.ID));

                    await Submit(userEdgeQuery);

                    var activityGroupEdgeQuery = g.V(activityGroupId).AddE(AmblOnGraphConstants.ContainsEdgeName).To(g.V(createdActivity.ID));

                    await Submit(activityGroupEdgeQuery);

                    if (activity.LocationID != null && activity.LocationID != Guid.Empty)
                    {
                        var locationEdgeQuery = g.V(createdActivity.ID).AddE(AmblOnGraphConstants.OccursAtEdgeName).To(g.V(activity.LocationID));

                        await Submit(locationEdgeQuery);
                    }

                    return new BaseResponse<Guid>()
                    {
                        Model = createdActivity.ID.Value,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { 
                        Model = existingActivity.ID.Value,
                        Status = Status.Conflict.Clone("An activity with that title already exists for this user's itinerary and activity group.")
                    };
            });
        }

        public virtual async Task<BaseResponse<Guid>> AddActivityGroup(string email, string entAPIKey, Guid itineraryId, ActivityGroup activityGroup)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|" + itineraryId.ToString() + "|" + activityGroup.Title.Replace(" ", "_");

                var existingActivityGroupQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itineraryId)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ActivityGroupVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
                var existingActivityGroups = await Submit<ActivityGroup>(existingActivityGroupQuery);

                var existingActivityGroup = existingActivityGroups?.FirstOrDefault();

                if (existingActivityGroup == null)
                {
                    var createQuery = g.AddV(AmblOnGraphConstants.ActivityGroupVertexName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, "ActivityGroup|" + activityGroup.Title.Substring(0,1))
                        .Property("Lookup", lookup)
                        .Property("GroupType", activityGroup.GroupType ?? "")
                        .Property("CreatedDateTime", activityGroup.CreatedDateTime)
                        .Property("Order", activityGroup.Order)
                        .Property("Checked", activityGroup.Checked)
                        .Property("Title", activityGroup.Title ?? "");

                    var createActivityGroupResults = await Submit<ActivityGroup>(createQuery);

                    var createdActivityGroup = createActivityGroupResults?.FirstOrDefault();

                    var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdActivityGroup.ID));

                    await Submit(userEdgeQuery);

                    var itineraryEdgeQuery = g.V(itineraryId).AddE(AmblOnGraphConstants.ContainsEdgeName).To(g.V(createdActivityGroup.ID));

                    await Submit(itineraryEdgeQuery);

                    return new BaseResponse<Guid>()
                    {
                        Model = createdActivityGroup.ID.Value,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { 
                        Model = existingActivityGroup.ID.Value,
                        Status = Status.Conflict.Clone("An activity group with that title already exists for this user's itinerary.")
                    };
            });
        }
        
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
                    return new BaseResponse<Guid>() { 
                        Model = existingAlbum.ID,
                        Status = Status.Conflict.Clone("An album with that title already exists for this user.")
                    };
            });
        }

        public virtual async Task<BaseResponse<Guid>> AddItinerary(string email, string entAPIKey, Itinerary itinerary)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = $"{userId.ToString()}|{itinerary.Title.Replace(" ", "_")}";

                var existingItineraryQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
                var existingItineraries = await Submit<Itinerary>(existingItineraryQuery);

                var existingItinerary = existingItineraries?.FirstOrDefault();

                if (existingItinerary == null)
                {
                    var createQuery = g.AddV(AmblOnGraphConstants.ItineraryVertexName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, "Itinerary|" + itinerary.Title.Substring(0,1))
                        .Property("Lookup", lookup)
                        .Property("CreatedDateTime", itinerary.CreatedDateTime)
                        .Property("Shared", itinerary.Shared)
                        .Property("SharedByUsername", "")
                        .Property("SharedByUserID", Guid.Empty)
                        .Property("Editable", itinerary.Editable)
                        .Property("Title", itinerary.Title ?? "");

                    var createItineraryResults = await Submit<Itinerary>(createQuery);

                    var createdItinerary = createItineraryResults?.FirstOrDefault();

                    var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdItinerary.ID));

                    await Submit(userEdgeQuery);

                    return new BaseResponse<Guid>()
                    {
                        Model = createdItinerary.ID.Value,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { 
                        Model = existingItinerary.ID.Value,
                        Status = Status.Conflict.Clone("An itinerary with that title already exists for this user.")
                    };
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
                    return new BaseResponse<Guid>() { 
                        Model = existingLayer.ID,
                        Status = Status.Conflict.Clone("A layer by that name already exists in for this user.")
                    };
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
                        .Property("IsHidden", location.IsHidden)
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
                    return new BaseResponse<Guid>() { 
                        Model = existingLocation.ID,
                        Status = Status.Conflict.Clone("A location by that lat/long already exists in selected layer.")                        
                    };
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
                    return new BaseResponse<Guid>() { 
                        Model = existingMap.ID,
                        Status = Status.Conflict.Clone("A map by that name already exists for this user.")
                    };
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
                    return new BaseResponse<Guid>() { 
                        Model = existingPhoto.ID,
                        Status = Status.Conflict.Clone("A photo for that user's album exists with the same URL.")
                    };
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
                    return new BaseResponse<Guid>() { 
                        Model = existingLayer.ID,
                        Status = Status.Conflict.Clone("A layer by that name already exists in for this user.")
                    };
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
                    return new BaseResponse<Guid>() { 
                        Model = existingMap.ID,
                        Status = Status.Conflict.Clone("A map by that name already exists for this user.")
                    };
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
                    return new BaseResponse<Guid>() { 
                        Model = existingMap.ID,
                        Status = Status.Conflict.Clone("A map by that name already exists for this user.")
                    };
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

                    var createTopList = await Submit<TopList>(createQuery);

                    var createdTopList = createTopList?.FirstOrDefault();

                    // Add edge to from user vertex to newly created top list vertex
                    var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdTopList.ID));
                    await Submit(userEdgeQuery);

                    // Add edges to each location - locations are presumed to already exist in the graph
                    foreach (UserLocation loc in topList.LocationList) {
                        if (loc.ID!=null) {
                            var locationQuery = g.V(createdTopList.ID).AddE(AmblOnGraphConstants.ContainsEdgeName).To(g.V(loc.ID));
                            await Submit(locationQuery);
                        }
                    }

                    return new BaseResponse<Guid>()
                    {
                        Model = createdTopList.ID,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { 
                        Model = existingTopList.ID,
                        Status = Status.Conflict.Clone("An top list with that title already exists for this user.")
                    };
            });
        }

        public virtual async Task<BaseResponse<Guid>> AddUserInfo(string email, string entAPIKey, UserInfo userInfo)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|UserInfo";

                var existingUserInfoQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.UserInfoVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
                var existingUserInfos = await Submit<UserInfo>(existingUserInfoQuery);

                var existingUserInfo = existingUserInfos?.FirstOrDefault();

                if (existingUserInfo == null)
                {
                    var partKey = email?.Split('@')[1];

                    var createQuery = g.AddV(AmblOnGraphConstants.UserInfoVertexName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, partKey)
                        .Property("Lookup", lookup)
                        .Property("Country", userInfo.Country ?? "")
                        .Property("FirstName", userInfo.FirstName ?? "")
                        .Property("LastName", userInfo.LastName ?? "")
                        .Property("Zip", userInfo.Zip);

                    var createUserInfo = await Submit<UserInfo>(createQuery);

                    var createdUserInfo = createUserInfo?.FirstOrDefault();

                    // Add edge to from user vertex to newly created top list vertex
                    var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdUserInfo.ID));
                    await Submit(userEdgeQuery);

                    return new BaseResponse<Guid>()
                    {
                        Model = createdUserInfo.ID.Value,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { 
                        Model = existingUserInfo.ID.Value,
                        Status = Status.Conflict.Clone("A User Info record already exists for this user.")
                    };
            });
        }
        #endregion

        #region Delete
        public virtual async Task<BaseResponse> DeleteAccolades(string email, string entAPIKey, Guid[] accoladeIDs, Guid locationId)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var existingAccoladeQuery = g.V(locationId)
                    .HasLabel(AmblOnGraphConstants.AccoladeVertexName);

                var existingAccolades = await Submit<Accolade>(existingAccoladeQuery);

                if (existingAccolades != null)
                {
                    var deleteQuery = g.V(locationId)
                     .Out(AmblOnGraphConstants.OwnsEdgeName)
                     .HasLabel(AmblOnGraphConstants.AccoladeVertexName)
                     .Has("ID", P.Inside(accoladeIDs))
                     .Drop();

                    await Submit(deleteQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This accolade does not exist") };
            });
        }

        public virtual async Task<BaseResponse> DeleteActivity(string email, string entAPIKey, Guid itineraryId, Guid activityGroupId, Guid activityId)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var existingActivityQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itineraryId)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ActivityGroupVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, activityGroupId)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ActivityVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, activityId);
                
                var existingActivities = await Submit<Activity>(existingActivityQuery);

                var existingActivity = existingActivities?.FirstOrDefault();

                if (existingActivity != null)
                {
                    var deleteActivityQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itineraryId)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ActivityGroupVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, activityGroupId)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ActivityVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, activityId)
                    .Drop();

                    await Submit(deleteActivityQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This activity does not exist for this user's itinerary/activity group")};
            });
        }

        public virtual async Task<BaseResponse> DeleteActivityGroup(string email, string entAPIKey, Guid itineraryId, Guid activityGroupId)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var existingActivityGroupQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itineraryId)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ActivityGroupVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, activityGroupId);
                
                var existingActivityGroups = await Submit<ActivityGroup>(existingActivityGroupQuery);

                var existingActivityGroup = existingActivityGroups?.FirstOrDefault();

                if (existingActivityGroup != null)
                {
                    var deleteActivityGroupQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itineraryId)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ActivityGroupVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, activityGroupId)
                    .Drop();

                    await Submit(deleteActivityGroupQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This activity group does not exist for this user's itinerary")};
            });
        }

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
                
                var existingTopLists = await Submit<TopList>(existingTopListQuery);

                // Get the top list vertex
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

        public virtual async Task<BaseResponse> DedupLocationsByMap(string email, string entAPIKey, Guid mapID)
        {
            return await withG(async (client, g) =>
            {
                var dedupeGuids = new List<Guid>();

                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                // Load all locations for a mapID
                var query = g.V(mapID)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.LocationVertexName);
                    
                var locationSet = await Submit<Location>(query);

                // Create collections of maps group by lat/lon (sufficient for "equality")
                var locationGroups = from l in locationSet.ToList()
                                    group l by new { Lat = l.Latitude, Lon = l.Longitude} into locGroup
                                    orderby locGroup.Key.Lat, locGroup.Key.Lon
                                    select locGroup;

                // For each group, take the guids for all but one.
                foreach(var locGroup in locationGroups) {
                    int locSize = locGroup.Count();
                    if (locSize > 1){
                        var locGuids =  locGroup.Select(l => l.ID)
                                                .Take(locSize-1)
                                                .ToArray();
                        dedupeGuids.AddRange(locGuids);
                    }
                } 
                
                // Delete the extraneous locations
                var dedupQuery = g.V(mapID)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.LocationVertexName)
                    .Has("id",  P.Within(dedupeGuids))
                    .Drop();

                var results = await Submit<Location>(dedupQuery);
               
                return new BaseResponse()
                {
                        Status = Status.Success
                };
            });
        }
        
        public virtual async Task<BaseResponse> DeleteMaps(string email, string entAPIKey, Guid[] mapIDs)
        {
            try {
            return await withG(async (client, g) =>
            {
                var stringGuids = mapIDs.Select(m => m.ToString()).ToArray();

                var userId = await ensureAmblOnUser(g, email, entAPIKey);
               
                var existingMapsQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.MapVertexName)
                    .Has("id",  P.Within(stringGuids));
   
                var existingMaps = await Submit<Map>(existingMapsQuery);

                if (existingMaps != null)
                {
                    var deleteQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.MapVertexName)
                    .Has("id",  P.Within(stringGuids))
                    .Drop();

                    await Submit(deleteQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("These maps do not exist for this user")};
              
            });
                          } catch (Exception ex) {
                   var result = ex.Message;
                   throw;
               }
        }

        #endregion

        #region Edit
        public virtual async Task<BaseResponse> EditActivity(string email, string entAPIKey, Activity activity)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var existingActivityQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ActivityVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, activity.ID);
                
                var existingActivities = await Submit<Activity>(existingActivityQuery);

                var existingActivity = existingActivities?.FirstOrDefault();

                if (existingActivity != null)
                {
                    var editQuery = g.V(existingActivity.ID)
                        .Property("Checked", activity.Checked)
                        .Property("CreatedDateTime", activity.CreatedDateTime)
                        .Property("Favorited", activity.Favorited)
                        .Property("Notes", activity.Notes ?? "")
                        .Property("Order", activity.Order)
                        .Property("Title", activity.Title ?? "")
                        .Property("TransportIcon", activity.TransportIcon ?? "")
                        .Property("WidgetIcon", activity.WidgetIcon ?? "");

                    var editActivityResults = await Submit<Activity>(editQuery);

                    var editedActivity = editActivityResults?.FirstOrDefault();

                    if (existingActivity.LocationID != activity.LocationID)
                    {
                         var deleteLocationEdgeQuery = g.V(activity.ID).OutE(AmblOnGraphConstants.OccursAtEdgeName).Drop();

                        await Submit(deleteLocationEdgeQuery);

                        if (activity.LocationID != null && activity.LocationID != Guid.Empty)
                        {
                            var locationEdgeQuery = g.V(activity.ID).AddE(AmblOnGraphConstants.OccursAtEdgeName).To(g.V(activity.LocationID));

                            await Submit(locationEdgeQuery);
                        }
                    }

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { 
                        Status = Status.Conflict.Clone("Activity not found.")
                    };
            });
        }

        public virtual async Task<BaseResponse> EditActivityGroup(string email, string entAPIKey, ActivityGroup activityGroup)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var existingActivityGroupQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ActivityGroupVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, activityGroup.ID);
                
                var existingActivityGroups = await Submit<ActivityGroup>(existingActivityGroupQuery);

                var existingActivityGroup = existingActivityGroups?.FirstOrDefault();

                if (existingActivityGroup != null)
                {
                    var editQuery = g.V(activityGroup.ID)
                        .Property("GroupType", activityGroup.GroupType ?? "")
                        .Property("CreatedDateTime", activityGroup.CreatedDateTime)
                        .Property("Order", activityGroup.Order)
                        .Property("Title", activityGroup.Title ?? "");

                    var editActivityGroupResults = await Submit<ActivityGroup>(editQuery);

                    var editedActivityGroup = editActivityGroupResults?.FirstOrDefault();

                    return new BaseResponse<Guid>()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { 
                        Status = Status.Conflict.Clone("Activity Group not found.")
                    };
            });
        }

        public virtual async Task<BaseResponse> EditAccolade(string email, string entAPIKey, UserAccolade accolade, Guid locationId)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = locationId.ToString() + "|" + accolade.Title.Replace(" ", "");

                var existingAccoladeQuery = g.V(locationId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.AccoladeVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, accolade.ID);

                var existingAccolades = await Submit<Accolade>(existingAccoladeQuery);

                var existingAccolade = existingAccolades?.FirstOrDefault();

                if (existingAccolades != null)
                {
                    var editQuery = g.V(accolade.ID)
                        .Property("Lookup", lookup)
                        .Property("Title", accolade.Title)
                        .Property("LocationId", accolade.LocationID)
                        .Property("Rank", accolade.Rank)
                        .Property("Year", accolade.Year);

                    await Submit(editQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This accolade does not exist for this layer") };
            });
        }
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
        public virtual async Task<BaseResponse> EditItinerary(string email, string entAPIKey, Itinerary itinerary)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = $"{userId.ToString()}|{itinerary.Title.Replace(" ", "_")}";

                var existingItineraryQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itinerary.ID);
                
                var existingItineraries = await Submit<Itinerary>(existingItineraryQuery);

                var existingItinerary = existingItineraries?.FirstOrDefault();

                if (existingItinerary != null)
                {
                    var editQuery = g.V(itinerary.ID)
                        .Property("CreatedDateTime", itinerary.CreatedDateTime)
                        .Property("Title", itinerary.Title ?? "")
                        .Property("Shared", itinerary.Shared)
                        .Property("SharedByUsername", itinerary.SharedByUsername ?? "")
                        .Property("SharedByUserID", itinerary.SharedByUserID)
                        .Property("Editable", itinerary.Editable)
                        .Property("Lookup", lookup);

                    var editItineraryResults = await Submit<Itinerary>(editQuery);

                    var editedItinerary = editItineraryResults?.FirstOrDefault();

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { 
                        Model = existingItinerary.ID.Value,
                        Status = Status.Conflict.Clone("Itinerary not found.")
                    };
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
                        .Property("IsHidden", location.IsHidden)
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

                // Retrieve the top list
                var existingTopList = existingTopLists?.FirstOrDefault();

                
                if (existingTopList != null)
                {    
                    // Update the top list properties                                
                    var editQuery = g.V(topList.ID)
                        .Property("Lookup", lookup)
                        .Property("Title", topList.Title ?? "")
                        .Property("OrderedValue", topList.OrderedValue);

                    
                    await Submit(editQuery);

                    // Delete existing edges
                    var existingTopListLocsQuery = g.V(existingTopList.ID)
                                            .OutE(AmblOnGraphConstants.ContainsEdgeName)
                                            .HasLabel(AmblOnGraphConstants.LocationVertexName)
                                            .Drop();

                    await Submit(existingTopListLocsQuery);

                    // Add new edges from ordered list 
                    foreach (UserLocation loc in topList.LocationList) {
                        var locationQuery = g.V(existingTopList.ID)
                                             .AddE(AmblOnGraphConstants.ContainsEdgeName)
                                             .To(g.V(loc.ID));
                        await Submit(locationQuery);
                    }

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This top list does not exist for this user")};
            });
        }

        public virtual async Task<BaseResponse> EditUserInfo(string email, string entAPIKey, UserInfo userInfo)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|UserInfo";

                var existingUserInfoQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.UserInfoVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, userInfo.ID);
                
                var existingUserInfos = await Submit<UserInfo>(existingUserInfoQuery);

                var existingUserInfo = existingUserInfos?.FirstOrDefault();

                if (existingUserInfo != null)
                {                               
                    var editQuery = g.V(userInfo.ID)
                        .Property("Country", userInfo.Country ?? "")
                        .Property("FirstName", userInfo.FirstName ?? "")
                        .Property("LastName", userInfo.LastName)
                        .Property("Zip", userInfo.Zip);

                    
                    await Submit(editQuery);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This User Info record does not exist for this user")};
            });
        }

        public virtual async Task<BaseResponse> EditExcludedCurations(string email, string entAPIKey, ExcludedCurations curations)
        {
            return await withG(async (client, g) =>
            {
                Guid excludedCurationsId;

                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var curationsExistsQuery = g.V(userId)
                                        .Out(AmblOnGraphConstants.OwnsEdgeName)
                                        .HasLabel("ExcludedCurations");

                var existsResult = await Submit<ExcludedCurations>(curationsExistsQuery);
                
                var existFirst = existsResult?.FirstOrDefault();

                if (existFirst == null) {
                    var createQuery = g.AddV(AmblOnGraphConstants.ExcludedCurationsName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, entAPIKey.ToString())
                        .Property("LocationIDs", curations.LocationIDs);

                    var createCurationsResults = await Submit<ExcludedCurations>(createQuery);

                    var createdCurations = createCurationsResults?.FirstOrDefault();

                    var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdCurations.ID));

                    await Submit(userEdgeQuery);

                    excludedCurationsId = createdCurations.ID;
                } else {
                    var updateQuery = g.V(existFirst.ID)
                        .Property("LocationIDs", curations.LocationIDs);

                    await Submit(updateQuery);

                    excludedCurationsId = existFirst.ID;
                }

                return new BaseResponse()
                {
                    Status = Status.Success
                };
            });
        }        
        #endregion 

        #region List
        public virtual async Task<BaseResponse<UserInfo>> GetUserInfo(string email, string entAPIKey)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var lookup = userId.ToString() + "|UserInfo";

                var existingUserInfoQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.UserInfoVertexName)
                    .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
                var existingUserInfos = await Submit<UserInfo>(existingUserInfoQuery);

                var existingUserInfo = existingUserInfos?.FirstOrDefault();

                if (existingUserInfo != null)
                {                               
                    return new BaseResponse<UserInfo>()
                    {
                        Status = Status.Success,
                        Model = existingUserInfo
                    };
                }
                else
                    return new BaseResponse<UserInfo>()
                    {
                        Status = Status.NotLocated
                    };
            });
        }
        public virtual async Task<List<Activity>> ListActivities(string email, string entAPIKey, Guid activityGroupId)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var query = g.V(activityGroupId)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ActivityVertexName);

                var results = await Submit<Activity>(query);

                results.ToList().ForEach(
                    (activity) =>
                    {
                        var locationId = getActivityLocationID(userId, activity.ID).GetAwaiter().GetResult();
                        activity.LocationID = locationId;
                    });

                return results.ToList();
            });
        }

        public virtual async Task<List<ActivityGroup>> ListActivityGroups(string email, string entAPIKey, Itinerary itinerary)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                // Check to see if the itinerary is shared. If shared, switch the "Out" part of the query to "CanView" instead of "Owns"
                var outVertexName = "";

                if(itinerary.Shared){
                    outVertexName = "CanView";               
                }
                else{
                    outVertexName = "Owns";
                }

                var query = g.V(userId)
                    .Out(outVertexName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itinerary.ID)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ActivityGroupVertexName);

                var results = await Submit<ActivityGroup>(query);

                return results.ToList();
            });
        }

        public virtual async Task<List<Accolade>> ListAccolades(string email, string entAPIKey, Guid locationId)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var query = g.V(locationId)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.AccoladeVertexName);

                var results = await Submit<Accolade>(query);

                return results.ToList();
            });
        }

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

                var ownedResults = await Submit<Itinerary>(query);

                var ownedList = ownedResults.ToList();

                ownedList.ForEach(
                    (owned) =>
                    {
                        owned.Shared = false;
                        owned.SharedByUserID = Guid.Empty;
                        owned.SharedByUsername = "";
                        owned.Editable = true;
                    });

                var sharedQuery = g.V(userId)
                      .Out(AmblOnGraphConstants.CanViewEdgeName)
                      .HasLabel(AmblOnGraphConstants.ItineraryVertexName);

                var sharedResults = await Submit<Itinerary>(sharedQuery);

                var sharedList = sharedResults.ToList();

                sharedList.ForEach(
                    (shared) =>
                    {
                        shared.Shared = true;
                        shared.Editable = false;

                        var userQuery = g.V(shared.ID)
                              .In(AmblOnGraphConstants.OwnsEdgeName)
                              .HasLabel(AmblOnGraphConstants.AmblOnUserVertexName);
                            
                        var userResults = Submit<AmblOnUser>(userQuery).GetAwaiter().GetResult();

                        var user = userResults?.FirstOrDefault();

                        var userInfoQuery = g.V(user.ID)
                                .Out(AmblOnGraphConstants.OwnsEdgeName)
                                .HasLabel(AmblOnGraphConstants.UserInfoVertexName);

                        var userInfoResults = Submit<UserInfo>(userInfoQuery).GetAwaiter().GetResult();

                        var userInfo = userInfoResults?.FirstOrDefault();

                        if (userInfo != null)
                        {
                            shared.SharedByUserID = user.ID;
                            shared.SharedByUsername = userInfo.FirstName + " " + userInfo.LastName;
                        }
                        else{
                            shared.SharedByUserID = user.ID;
                            shared.SharedByUsername = user.Email;
                        }
                    });

                var results = new List<Itinerary>();

                results.AddRange(ownedList);
                results.AddRange(sharedList);
                
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

        public virtual async Task<List<Location>> ListTopListLocations(string email, string entAPIKey, Guid topListID)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var query = g.V(topListID)
                    .Out(AmblOnGraphConstants.ContainsEdgeName)
                    .HasLabel(AmblOnGraphConstants.LocationVertexName);

                var results = await Submit<Location>(query);

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

        public virtual async Task<ExcludedCurations> ListExcludedCurations(string email, string entAPIKey)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var query = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ExcludedCurationsName);

                var results = await Submit<ExcludedCurations>(query);

                return results?.FirstOrDefault();
            });

        }
        #endregion

        public virtual async Task<BaseResponse> ShareItinerary(string email, string entAPIKey, Guid itineraryId, string shareWithUsername)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);
                var shareUserId = await ensureAmblOnUser(g, shareWithUsername, entAPIKey);

                var existingItineraryQuery = g.V(shareUserId)
                    .Out(AmblOnGraphConstants.CanViewEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itineraryId);
                
                var existingItineraries = await Submit<Itinerary>(existingItineraryQuery);

                var existingItinerary = existingItineraries?.FirstOrDefault();

                if (existingItinerary == null)
                {
                    var objectQuery = g.V(userId)
                        .Out(AmblOnGraphConstants.OwnsEdgeName)
                        .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                        .Has(AmblOnGraphConstants.IDPropertyName, itineraryId);

                    var existingObjects = await Submit<Itinerary>(objectQuery);

                    var existingObject = existingObjects?.FirstOrDefault();

                    if (existingObject != null)
                    {
                        var userEdgeQuery = g.V(shareUserId).AddE(AmblOnGraphConstants.CanViewEdgeName).To(g.V(itineraryId));

                        await Submit(userEdgeQuery);

                        return new BaseResponse()
                        {
                            Status = Status.Success
                        };
                    }
                    else
                        return new BaseResponse() { 
                        Status = Status.Conflict.Clone("Itinerary not found.")
                    };
                }
                else
                    return new BaseResponse() { 
                        Status = Status.Conflict.Clone("Itinerary is already shared with this user.")
                    };
            });
        }

        public virtual async Task<BaseResponse> UnshareItinerary(string email, string entAPIKey, Guid itineraryId, string shareWithUsername)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email, entAPIKey);

                var existingItineraryQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.CanViewEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, itineraryId);
                
                var existingItineraries = await Submit<Itinerary>(existingItineraryQuery);

                var existingItinerary = existingItineraries?.FirstOrDefault();

                if (existingItinerary != null)
                {
                    var objectQuery = g.V(itineraryId);

                    var existingObjects = await Submit<Itinerary>(objectQuery);

                    var existingObject = existingObjects?.FirstOrDefault();

                    if (existingObject != null)
                    {
                        var userEdgeQuery = g.V(userId).Out(AmblOnGraphConstants.CanViewEdgeName).To(g.V(itineraryId)).Drop();

                        await Submit(userEdgeQuery);

                        return new BaseResponse()
                        {
                            Status = Status.Success
                        };
                    }
                    else
                        return new BaseResponse() { 
                        Status = Status.Conflict.Clone("Itinerary not found.")
                    };
                }
                else
                    return new BaseResponse() { 
                        Status = Status.Conflict.Clone("Itinerary is not shared with this user.")
                    };
            });
        }
        
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

        public virtual async Task<Guid> getActivityLocationID(Guid userId, Guid? activityId)
        {
            return await withG(async (client, g) =>
            {
                var query = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ActivityVertexName)
                    .Has(AmblOnGraphConstants.IDPropertyName, activityId.HasValue ? activityId.Value : Guid.Empty)
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
                Title = "User"
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
                Title = "Global",
                Deletable = false,
                DefaultLayerID = sharedLayerResult
            }, false, sharedMapResult);

            await AddSharedLayer(email, entAPIKey, new UserLayer()
            {
                Title = "Curated",
                Deletable = false,
            }, false, sharedLayerResult, sharedMapResult);

            return user;
        }
        #endregion
    }
}