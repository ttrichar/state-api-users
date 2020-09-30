using AmblOn.State.API.Users.Models;
using Fathym;
using Fathym.API;
using LCU.Graphs;
using Gremlin.Net.Process.Traversal;
using Gremlin.Net.Driver;
using LCU.Graphs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ExRam.Gremlinq.Core;
using LCU.Graphs.Registry.Enterprises.Edges;

namespace AmblOn.State.API.Users.Graphs
{
    public class AmblOnGraph : LCUGraph
    {
        #region Properties

        #endregion

        #region Constructors
        public AmblOnGraph(LCUGraphConfig graphConfig, ILogger<AmblOnGraph> logger)
            : base(graphConfig, logger)
        { }
        #endregion

        #region API Methods 

        #region Add 
        // public virtual async Task<BaseResponse<Guid>> AddAccolade(string email, string entLookup, UserAccolade accolade, Guid locationId)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var lookup = locationId.ToString() + "|" + accolade.Title.Replace(" ", "").Replace("'","");

        //         // Look up the accolade in the layer (curated layer, by default)
        //         var existingAccoladeQuery = g.V(locationId)
        //             .Out(AmblOnGraphConstants.ContainsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.AccoladeVertexName)
        //             .Has(AmblOnGraphConstants.LookupPropertyName, lookup);

        //         var existingAccolades = await Submit<Accolade>(existingAccoladeQuery);

        //         var existingAccolade = existingAccolades?.FirstOrDefault();

        //         if (existingAccolade == null)
        //         {
        //             // Add the accolade vertex
        //             var createQuery = g.AddV(AmblOnGraphConstants.AccoladeVertexName)
        //                 .Property(AmblOnGraphConstants.PartitionKeyName, entLookup.ToString())
        //                 .Property("Lookup", lookup)
        //                 .Property("Title", accolade.Title ?? "")
        //                 .Property("Year", accolade.Year ?? "")
        //                 .Property("Rank", accolade.Rank ?? "");

        //             var createAccolade = await Submit<Accolade>(createQuery);

        //             var createdAccolade = createAccolade?.FirstOrDefault();
        //             createdAccolade.LocationID = locationId;

        //             // Add edge from location vertex to newly created accolade vertex (Contains)
        //             var locationEdgeQuery = g.V(locationId).AddE(AmblOnGraphConstants.ContainsEdgeName).To(g.V(createdAccolade.ID));
        //             await Submit(locationEdgeQuery);

        //             return new BaseResponse<Guid>()
        //             {
        //                 Model = createdAccolade.ID,
        //                 Status = Status.Success
        //             };
        //         }
        //         else
        //             return new BaseResponse<Guid>() { 
        //                 Model = existingAccolade.ID,
        //                 Status = Status.Conflict.Clone("An accolade with that title already exists for this layer.") 
        //             };
        //     });
        // }

        public virtual async Task<BaseResponse<Guid>> AddActivityToAG(string email, string entLookup, Guid itineraryId, Guid activityGroupId, Activity activity)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var lookup = userId.ToString() + "|" + itineraryId.ToString() + "|" + activityGroupId.ToString() + "|" + activity.Title.Replace(" ", "_") + "|" + 
            (activity.LocationID.HasValue ? activity.LocationID.Value.ToString() : Guid.Empty.ToString()) + "|" + activity.Order.ToString();

            var activityID = activity.ID;

            var existingActivity = await g.V(userId)
                .Out<Owns>()
                .OfType<Itinerary>()
                .Where(e => e.ID == itineraryId)
                .Out<Contains>()
                .OfType<ActivityGroup>()
                .Where(e => e.ID == activityGroupId)
                .Out<Contains>()
                .OfType<Activity>()
                .Where(e => e.Lookup == lookup)
                .FirstOrDefaultAsync();            

            if (existingActivity == null)
            {            
                var existingActivityByID = await g.V(userId)
                    .Out<Owns>()
                    .OfType<Itinerary>()
                    .Where(e => e.ID == itineraryId)
                    .Out<Contains>()
                    .OfType<ActivityGroup>()
                    .Where(e => e.ID == activityGroupId)
                    .Out<Contains>()
                    .OfType<Activity>()
                    .Where(e => e.ID == activityID)
                    .FirstOrDefaultAsync();     

                if(existingActivityByID == null)
                {
                    var createdActivity = await g.AddV<Activity>(new Activity(){
                        Lookup = lookup, 
                        CreatedDateTime = activity.CreatedDateTime,
                        LocationID = activity.LocationID ?? Guid.Empty,
                        Order = activity.Order,
                        Notes = activity.Notes ?? "",
                        Checked = activity.Checked,
                        Favorited = activity.Favorited,
                        Title = activity.Title ?? "",
                        TransportIcon = activity.TransportIcon ?? "",
                        WidgetIcon = activity.TransportIcon ?? ""
                    })
                    .FirstOrDefaultAsync();

                    await g.V(userId)
                        .AddE<Owns>()
                        .To(x => x.V(createdActivity.ID))
                        .FirstOrDefaultAsync();

                    await g.V(activityGroupId)
                        .AddE<Contains>()
                        .To(x => x.V(createdActivity.ID))
                        .FirstOrDefaultAsync();

                    if (activity.LocationID != null && activity.LocationID != Guid.Empty)
                    {
                        await g.V(createdActivity.ID)
                            .AddE<OccursAt>()
                            .To(x => x.V(activity.LocationID))
                            .FirstOrDefaultAsync();
                    }

                    return new BaseResponse<Guid>()
                    {
                        Model = createdActivity.ID,
                        Status = Status.Success
                    };
                }
                else{
                    var editResp = await EditActivity(email, entLookup, activity);

                    return new BaseResponse<Guid>() { 
                        Model = existingActivityByID.ID,
                        //Status = Status.Conflict.Clone("An activity with that title already exists for this user's itinerary and activity group.")
                        Status = Status.Success
                    };
                }

            }
            else{
                var editResp = await EditActivity(email, entLookup, activity);
            
                return new BaseResponse<Guid>() { 
                    Model = existingActivity.ID,
                    //Status = Status.Conflict.Clone("An activity with that title already exists for this user's itinerary and activity group.")
                    Status = Status.Success
                };
            }
        }

        public virtual async Task<BaseResponse<Guid>> AddActivityGroup(string email, string entLookup, Guid itineraryId, ActivityGroup activityGroup)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var lookup = userId.ToString() + "|" + itineraryId.ToString() + "|" + activityGroup.Title.Replace(" ", "_");

            var existingActivityGroup = await g.V(userId)
                .Out<Owns>()
                .OfType<Itinerary>()
                .Where(e => e.ID == itineraryId)
                .Out<Contains>()
                .OfType<ActivityGroup>()
                .Where(e => e.Lookup == activityGroup.Lookup)
                .FirstOrDefaultAsync();

            if (existingActivityGroup == null)
            {

                var createdActivityGroup = await g.AddV<ActivityGroup>(new ActivityGroup(){
                    Lookup = lookup, 
                    CreatedDateTime = activityGroup.CreatedDateTime,
                    GroupType = activityGroup.GroupType ?? "",
                    Order = activityGroup.Order,
                    Checked = activityGroup.Checked,
                    Title = activityGroup.Title ?? ""
                })
                .FirstOrDefaultAsync();

                await g.V(userId)
                    .AddE<Owns>()
                    .To(x => x.V(createdActivityGroup.ID))
                    .FirstOrDefaultAsync();

                await g.V(itineraryId)
                    .AddE<Contains>()
                    .To(x => x.V(createdActivityGroup.ID))
                    .FirstOrDefaultAsync();

                return new BaseResponse<Guid>()
                {
                    Model = createdActivityGroup.ID,
                    Status = Status.Success
                };
            }
            else
                return new BaseResponse<Guid>() { 
                    Model = existingActivityGroup.ID,
                    Status = Status.Conflict.Clone("An activity group with that title already exists for this user's itinerary.")
                };
        }
        
        public virtual async Task<BaseResponse<Guid>> AddAlbum(string email, string entLookup, Album album)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var lookup = userId.ToString() + "|" + album.Title.Replace(" ","");

            var existingAlbum = await g.V<Album>(userId)
                .Out<Owns>()
                .OfType<Album>()
                .Where(e => e.ID == album.ID)
                .FirstOrDefaultAsync();

            if (existingAlbum == null)
            {
                var createdAlbum = await g.AddV<Album>(new Album(){
                    Lookup = lookup, 
                    Title = album.Title ?? ""
                })
                .FirstOrDefaultAsync();

                await g.V(userId)
                    .AddE<Owns>()
                    .To(x => x.V(createdAlbum.ID))
                    .FirstOrDefaultAsync();

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
        }

        public virtual async Task<BaseResponse<Guid>> AddItinerary(string email, string entLookup, Itinerary itinerary)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var lookup = $"{userId.ToString()}|{itinerary.Title.Replace(" ", "_")}";

            var existingItinerary = await g.V<Itinerary>(userId)
                .Out<Owns>()
                .OfType<Itinerary>()
                .Where(x => x.ID == itinerary.ID)
                .FirstOrDefaultAsync();

            if (existingItinerary == null)
            {
                var createdItinerary = await g.AddV<Itinerary>(new Itinerary(){
                    Lookup = lookup, 
                    CreatedDateTime = itinerary.CreatedDateTime,
                    Shared = itinerary.Shared,
                    SharedByUsername = "",
                    SharedByUserID = Guid.Empty,
                    Editable = itinerary.Editable,
                    Title = itinerary.Title ?? ""
                })
                .FirstOrDefaultAsync();

                await g.V(userId)
                    .AddE<Owns>()
                    .To(x => x.V(createdItinerary.ID))
                    .FirstOrDefaultAsync();

                return new BaseResponse<Guid>()
                {
                    Model = createdItinerary.ID,
                    Status = Status.Success
                };
            }
            else
                return new BaseResponse<Guid>() { 
                    Model = existingItinerary.ID,
                    Status = Status.Conflict.Clone("An itinerary with that title already exists for this user.")
                };
        }

        // public virtual async Task<BaseResponse<Guid>> AddLayer(string email, string entLookup, UserLayer layer)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var lookup = userId.ToString() + "|" + layer.Title.Replace(" ","");
                
        //         var existingLayerQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.LayerVertexName)
        //             .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
        //         var existingLayers = await Submit<Layer>(existingLayerQuery);

        //         var existingLayer = existingLayers?.FirstOrDefault();

        //         if (existingLayer == null)
        //         {
        //             var createQuery = g.AddV(AmblOnGraphConstants.LayerVertexName)
        //                 .Property(AmblOnGraphConstants.PartitionKeyName, entLookup.ToString())
        //                 .Property("Lookup", lookup)
        //                 .Property("Title", layer.Title);

        //             var createLayerResults = await Submit<Layer>(createQuery);

        //             var createdLayer = createLayerResults?.FirstOrDefault();

        //             var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdLayer.ID));

        //             await Submit(userEdgeQuery);

        //             return new BaseResponse<Guid>()
        //             {
        //                 Model = createdLayer.ID,
        //                 Status = Status.Success
        //             };
        //         }
        //         else
        //             return new BaseResponse<Guid>() { 
        //                 Model = existingLayer.ID,
        //                 Status = Status.Conflict.Clone("A layer by that name already exists in for this user.")
        //             };
        //     });
        // }

        // public virtual async Task<BaseResponse<Guid>> AddLocation(string email, string entLookup, UserLocation location)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var lookup = location.LayerID.ToString() + "|" + location.Latitude.ToString() + "|" + location.Longitude.ToString();

        //         var existingLocationQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.LayerVertexName)
        //             .Has(AmblOnGraphConstants.IDPropertyName, location.LayerID)
        //             .Out(AmblOnGraphConstants.ContainsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.LocationVertexName)
        //             .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
        //         var existingLocations = await Submit<Location>(existingLocationQuery);

        //         var existingLocation = existingLocations?.FirstOrDefault();

        //         if (existingLocation == null)
        //         {
        //             var createQuery = g.AddV(AmblOnGraphConstants.LocationVertexName)
        //                 .Property(AmblOnGraphConstants.PartitionKeyName, Convert.ToInt32(location.Latitude).ToString() + Convert.ToInt32(location.Longitude).ToString())
        //                 .Property("Lookup", lookup)
        //                 .Property("Address", location.Address ?? "")
        //                 .Property("Country", location.Country ?? "")
        //                 .Property("GoogleLocationName", location.GoogleLocationName ?? "")
        //                 .Property("Icon", location.Icon ?? "")
        //                 .Property("Instagram", location.Instagram ?? "")
        //                 .Property("IsHidden", location.IsHidden)
        //                 .Property("Latitude", location.Latitude)
        //                 .Property("Longitude", location.Longitude)
        //                 .Property("State", location.State ?? "")
        //                 .Property("Telephone", location.Telephone ?? "")
        //                 .Property("Title", location.Title ?? "")
        //                 .Property("Town", location.Town ?? "")
        //                 .Property("Website", location.Website ?? "")
        //                 .Property("ZipCode", location.ZipCode ?? "");

        //             var createLocationResults = await Submit<Location>(createQuery);

        //             var createdLocation = createLocationResults?.FirstOrDefault();

        //             var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdLocation.ID));

        //             await Submit(userEdgeQuery);

        //             var layerEdgeQuery = g.V(location.LayerID).AddE(AmblOnGraphConstants.ContainsEdgeName).To(g.V(createdLocation.ID));

        //             await Submit(layerEdgeQuery);

        //             return new BaseResponse<Guid>()
        //             {
        //                 Model = createdLocation.ID.Value,
        //                 Status = Status.Success
        //             };
        //         }
        //         else
        //             return new BaseResponse<Guid>() { 
        //                 Model = existingLocation.ID.Value,
        //                 Status = Status.Conflict.Clone("A location by that lat/long already exists in selected layer.")                        
        //             };
        //     });
        // }

        // public virtual async Task<BaseResponse<Guid>> AddMap(string email, string entLookup, UserMap map)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var lookup = userId.ToString() + "|" + map.Title.Replace(" ","");

        //         var existingMapQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.MapVertexName)
        //             .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
        //         var existingMaps = await Submit<Map>(existingMapQuery);

        //         var existingMap = existingMaps?.FirstOrDefault();

        //         if (existingMap == null)
        //         {
        //             var createQuery = g.AddV(AmblOnGraphConstants.MapVertexName)
        //                 .Property(AmblOnGraphConstants.PartitionKeyName, entLookup.ToString())
        //                 .Property("Lookup", lookup)
        //                 .Property("Title", map.Title)
        //                 .Property("Zoom", map.Zoom)
        //                 .Property("Latitude", map.Latitude)
        //                 .Property("Longitude", map.Longitude)
        //                 .Property("Primary", true)
        //                 .Property("Coordinates", String.Join(",", map.Coordinates))
        //                 .Property("DefaultLayerID", map.DefaultLayerID);

        //             var createMapResults = await Submit<Map>(createQuery);

        //             var createdMap = createMapResults?.FirstOrDefault();

        //             var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdMap.ID));

        //             await Submit(userEdgeQuery);

        //             await setPrimaryMap(email, entLookup, createdMap.ID);

        //             return new BaseResponse<Guid>()
        //             {
        //                 Model = createdMap.ID,
        //                 Status = Status.Success
        //             };
        //         }
        //         else
        //             return new BaseResponse<Guid>() { 
        //                 Model = existingMap.ID,
        //                 Status = Status.Conflict.Clone("A map by that name already exists for this user.")
        //             };
        //     });
        // }

        public virtual async Task<BaseResponse<Guid>> AddPhoto(string email, string entLookup, Photo photo, Guid albumID)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var lookup = userId.ToString() + "|" + albumID.ToString() + "|" + photo.URL;

            var existingPhoto = await g.V(userId)
                .Out<Owns>()
                .OfType<Album>()
                .Where(x => x.ID == albumID)
                .Out<Contains>()
                .OfType<Photo>()
                .Where(x => x.ID == photo.ID)
                .FirstOrDefaultAsync();

            if (existingPhoto == null)
            {
                var createdPhoto = await g.AddV<Photo>(new Photo(){
                    Lookup = lookup, 
                    Caption = photo.Caption ?? "",
                    URL = photo.URL ?? "",
                })
                .FirstOrDefaultAsync();

                await g.V(userId)
                    .AddE<Owns>()
                    .To(x => x.V(createdPhoto.ID))
                    .FirstOrDefaultAsync();;
                    
                await g.V(albumID)
                    .AddE<Contains>()
                    .To(x => x.V(createdPhoto.ID))
                    .FirstOrDefaultAsync();;

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
        }
        
        // public virtual async Task<BaseResponse<Guid>> AddSharedLayer(string email, string entLookup, UserLayer layer, bool deletable, Guid parentID, Guid defaultMapID)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var lookup = userId.ToString() + "|" + layer.Title.Replace(" ","");
                
        //         var existingLayerQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.SharedLayerVertexName)
        //             .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
        //         var existingLayers = await Submit<SharedLayer>(existingLayerQuery);

        //         var existingLayer = existingLayers?.FirstOrDefault();

        //         if (existingLayer == null)
        //         {
        //             var createQuery = g.AddV(AmblOnGraphConstants.SharedLayerVertexName)
        //                 .Property(AmblOnGraphConstants.PartitionKeyName, entLookup.ToString())
        //                 .Property("Lookup", lookup)
        //                 .Property("Title", layer.Title)
        //                 .Property("DefaultMapID", defaultMapID)
        //                 .Property("Deletable", deletable);

        //             var createLayerResults = await Submit<SharedLayer>(createQuery);

        //             var createdLayer = createLayerResults?.FirstOrDefault();

        //             var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdLayer.ID));

        //             await Submit(userEdgeQuery);

        //             var parentEdgeQuery = g.V(createdLayer.ID).AddE(AmblOnGraphConstants.InheritsEdgeName).To(g.V(parentID));

        //             await Submit(parentEdgeQuery);

        //             return new BaseResponse<Guid>()
        //             {
        //                 Model = createdLayer.ID,
        //                 Status = Status.Success
        //             };
        //         }
        //         else
        //             return new BaseResponse<Guid>() { 
        //                 Model = existingLayer.ID,
        //                 Status = Status.Conflict.Clone("A layer by that name already exists in for this user.")
        //             };
        //     });
        // }

        // public virtual async Task<BaseResponse<Guid>> AddSharedMap(string email, string entLookup, UserMap map, Guid parentID)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var lookup = userId.ToString() + "|" + map.Title.Replace(" ","");

        //         var existingMapQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.SharedMapVertexName)
        //             .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
        //         var existingMaps = await Submit<SharedMap>(existingMapQuery);

        //         var existingMap = existingMaps?.FirstOrDefault();

        //         if (existingMap == null)
        //         {
        //             var createQuery = g.AddV(AmblOnGraphConstants.SharedMapVertexName)
        //                 .Property(AmblOnGraphConstants.PartitionKeyName, entLookup.ToString())
        //                 .Property("Lookup", lookup)
        //                 .Property("Title", map.Title)
        //                 .Property("Deletable", true)
        //                 .Property("Primary", true)
        //                 .Property("DefaultLayerID", map.DefaultLayerID);

        //             var createMapResults = await Submit<SharedMap>(createQuery);

        //             var createdMap = createMapResults?.FirstOrDefault();

        //             var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdMap.ID));

        //             await Submit(userEdgeQuery);

        //             var parentEdgeQuery = g.V(createdMap.ID).AddE(AmblOnGraphConstants.InheritsEdgeName).To(g.V(parentID));

        //             await Submit(parentEdgeQuery);

        //             await setPrimaryMap(email, entLookup, createdMap.ID);

        //             return new BaseResponse<Guid>()
        //             {
        //                 Model = createdMap.ID,
        //                 Status = Status.Success
        //             };
        //         }
        //         else
        //             return new BaseResponse<Guid>() { 
        //                 Model = existingMap.ID,
        //                 Status = Status.Conflict.Clone("A map by that name already exists for this user.")
        //             };
        //     });
        // }

        // public virtual async Task<BaseResponse<Guid>> AddSharedMap(string email, string entLookup, SharedMap map, bool deletable, Guid parentID)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var lookup = userId.ToString() + "|" + map.Title.Replace(" ","");

        //         var existingMapQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.SharedMapVertexName)
        //             .Has(AmblOnGraphConstants.LookupPropertyName, lookup);
                
        //         var existingMaps = await Submit<SharedMap>(existingMapQuery);

        //         var existingMap = existingMaps?.FirstOrDefault();

        //         if (existingMap == null)
        //         {
        //             var createQuery = g.AddV(AmblOnGraphConstants.SharedMapVertexName)
        //                 .Property(AmblOnGraphConstants.PartitionKeyName, entLookup.ToString())
        //                 .Property("Lookup", lookup)
        //                 .Property("Primary", true)
        //                 .Property("Title", map.Title)
        //                 .Property("Deletable", deletable)
        //                 .Property("DefaultLayerID", map.DefaultLayerID);

        //             var createMapResults = await Submit<SharedMap>(createQuery);

        //             var createdMap = createMapResults?.FirstOrDefault();

        //             var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdMap.ID));

        //             await Submit(userEdgeQuery);

        //             var parentEdgeQuery = g.V(createdMap.ID).AddE(AmblOnGraphConstants.InheritsEdgeName).To(g.V(parentID));

        //             await Submit(parentEdgeQuery);

        //             await setPrimaryMap(email, entLookup, createdMap.ID);

        //             return new BaseResponse<Guid>()
        //             {
        //                 Model = createdMap.ID,
        //                 Status = Status.Success
        //             };
        //         }
        //         else
        //             return new BaseResponse<Guid>() { 
        //                 Model = existingMap.ID,
        //                 Status = Status.Conflict.Clone("A map by that name already exists for this user.")
        //             };
        //     });
        // }

        public virtual async Task<BaseResponse<Guid>> AddTopList(string email, string entLookup, UserTopList topList)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var lookup = userId.ToString() + "|" + topList.Title.Replace(" ","");

            var existingTopList = await g.V(userId)
                .Out<Owns>()
                .OfType<TopList>()
                .Where(x => x.ID == topList.ID)
                .FirstOrDefaultAsync();

            if (existingTopList == null)
            {
                // Add the Top List
                var createdTopList = await g.AddV<TopList>(new TopList(){
                    Lookup = lookup, 
                    Title = topList.Title ?? "",
                    OrderedValue = topList.OrderedValue ?? "",
                })
                .FirstOrDefaultAsync();

                // Add edge to from user vertex to newly created top list vertex
                await g.V(userId)
                    .AddE<Owns>()
                    .To(x => x.V(createdTopList.ID))
                    .FirstOrDefaultAsync();;

                // Add edges to each location - locations are presumed to already exist in the graph
                foreach (UserLocation loc in topList.LocationList) {
                    if (loc.ID!=null) {
                        await g.V(createdTopList.ID)
                            .AddE<Contains>()
                            .To(x => x.V(loc.ID))
                            .FirstOrDefaultAsync();                   
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
        }

        public virtual async Task<BaseResponse<Guid>> AddUserInfo(string email, string entLookup, UserInfo userInfo)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var lookup = userId.ToString() + "|UserInfo";

            var existingUserInfo = await g.V(userId)
                .Out<Owns>()
                .OfType<UserInfo>()
                .Where(x => x.Lookup == lookup)
                .FirstOrDefaultAsync();

            if (existingUserInfo == null)
            {
                var partKey = email?.Split('@')[1];

                var createdUserInfo = await g.AddV<UserInfo>(new UserInfo(){
                    Lookup = lookup, 
                    Email = email,
                    Country = userInfo.Country ?? "",
                    FirstName = userInfo.FirstName ?? "",
                    LastName = userInfo.LastName ?? "",
                    Zip = userInfo.Zip ?? ""
                })
                .FirstOrDefaultAsync();

                // Add edge to from user vertex to newly created top list vertex
                await g.V(userId)
                    .AddE<Owns>()
                    .To(x => x.V(createdUserInfo.ID))
                    .FirstOrDefaultAsync();

                return new BaseResponse<Guid>()
                {
                    Model = createdUserInfo.ID,
                    Status = Status.Success
                };
            }
            else
                return new BaseResponse<Guid>() { 
                    Model = existingUserInfo.ID,
                    Status = Status.Conflict.Clone("A User Info record already exists for this user.")
                };
        }
        #endregion

        #region Delete
        // public virtual async Task<BaseResponse> DeleteAccolades(string email, string entLookup, Guid[] accoladeIDs, Guid locationId)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var existingAccoladeQuery = g.V(locationId)
        //             .HasLabel(AmblOnGraphConstants.AccoladeVertexName);

        //         var existingAccolades = await Submit<Accolade>(existingAccoladeQuery);

        //         if (existingAccolades != null)
        //         {
        //             var deleteQuery = g.V(locationId)
        //              .Out(AmblOnGraphConstants.OwnsEdgeName)
        //              .HasLabel(AmblOnGraphConstants.AccoladeVertexName)
        //              .Has("ID", P.Inside(accoladeIDs))
        //              .Drop();

        //             await Submit(deleteQuery);

        //             return new BaseResponse()
        //             {
        //                 Status = Status.Success
        //             };
        //         }
        //         else
        //             return new BaseResponse() { Status = Status.NotLocated.Clone("This accolade does not exist") };
        //     });
        // }

        public virtual async Task<BaseResponse> DeleteActivity(string email, string entLookup, Guid itineraryId, Guid activityGroupId, Guid activityId)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var existingActivity = await g.V(userId)
                .Out<Owns>()
                .OfType<Itinerary>()
                .Where(e => e.ID == itineraryId)
                .Out<Contains>()
                .OfType<ActivityGroup>()
                .Where(e => e.ID == activityGroupId)
                .Out<Contains>()
                .OfType<Activity>()
                .Where(e => e.ID == activityId)                    
                .FirstOrDefaultAsync();

            if (existingActivity != null)
            {
                await g.V(userId)
                    .Out<Owns>()
                    .OfType<Itinerary>()
                    .Where(e => e.ID == itineraryId)
                    .Out<Contains>()
                    .OfType<ActivityGroup>()
                    .Where(e => e.ID == activityGroupId)
                    .Out<Contains>()
                    .OfType<Activity>()
                    .Where(e => e.ID == activityId)
                    .Drop();

                return new BaseResponse()
                {
                    Status = Status.Success
                };
            }
            else
                return new BaseResponse() { Status = Status.NotLocated.Clone("This activity does not exist for this user's itinerary/activity group")};
        }

        public virtual async Task<BaseResponse> DeleteActivityGroup(string email, string entLookup, Guid itineraryId, Guid activityGroupId)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var existingActivityGroup = await g.V(userId)
                .Out<Owns>()
                .OfType<Itinerary>()
                .Where(e => e.ID == itineraryId)
                .Out<Contains>()
                .OfType<ActivityGroup>()
                .Where(e => e.ID == activityGroupId)
                .FirstOrDefaultAsync();

            if (existingActivityGroup != null)
            {
                await g.V(userId)
                    .Out<Owns>()
                    .OfType<Itinerary>()
                    .Where(e => e.ID == itineraryId)
                    .Out<Contains>()
                    .OfType<ActivityGroup>()
                    .Where(e => e.ID == activityGroupId)
                    .Drop();

                return new BaseResponse()
                {
                    Status = Status.Success
                };
            }
            else
                return new BaseResponse() { Status = Status.NotLocated.Clone("This activity group does not exist for this user's itinerary")};

        }

        public virtual async Task<BaseResponse> DeleteAlbum(string email, string entLookup, Guid albumID)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var existingAlbum = await g.V<Album>(userId)
                .Out<Owns>()
                .OfType<Album>()
                .Where(e => e.ID == albumID)
                .FirstOrDefaultAsync();

            if (existingAlbum != null)
            {
                await g.V<Itinerary>(userId)
                    .Out<Owns>()
                    .OfType<Album>()
                    .Where(x => x.ID == albumID)
                    .Out<Contains>()
                    .OfType<Photo>()
                    .Drop();

                await g.V<Itinerary>(userId)
                    .Out<Owns>()
                    .OfType<Album>()
                    .Where(x => x.ID == albumID)
                    .Drop();

                return new BaseResponse()
                {
                    Status = Status.Success
                };
            }
            else
                return new BaseResponse() { Status = Status.NotLocated.Clone("This album does not exist for this user")};
        }

        public virtual async Task<BaseResponse> DeleteItinerary(string email, string entLookup, Guid itineraryID)
        {
            var userId = await ensureAmblOnUser(email, entLookup);
            
            var existingItinerary = await g.V<Itinerary>(userId)
                .Out<Owns>()
                .OfType<Itinerary>()
                .Where(x => x.ID == itineraryID)
                .FirstOrDefaultAsync();

            if (existingItinerary != null)
            {
                await g.V<Itinerary>(userId)
                .Out<Owns>()
                .OfType<Itinerary>()
                .Where(x => x.ID == itineraryID)
                .Drop();

                return new BaseResponse()
                {
                    Status = Status.Success
                };
            }
            else
                return new BaseResponse() { Status = Status.NotLocated.Clone("This itinerary does not exist for this user")};
        }

        // public virtual async Task<BaseResponse> DeleteLocation(string email, string entLookup, Guid locationID)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var existingLocationQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.LocationVertexName)
        //             .Has(AmblOnGraphConstants.IDPropertyName, locationID);
                
        //         var existingLocations = await Submit<Location>(existingLocationQuery);

        //         var existingLocation = existingLocations?.FirstOrDefault();

        //         if (existingLocation != null)
        //         {
        //             var deleteQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.LocationVertexName)
        //             .Has(AmblOnGraphConstants.IDPropertyName, locationID)
        //             .Drop();

        //             await Submit(deleteQuery);

        //             return new BaseResponse()
        //             {
        //                 Status = Status.Success
        //             };
        //         }
        //         else
        //             return new BaseResponse() { Status = Status.NotLocated.Clone("This location does not exist in the user's layer")};
        //     });
        // }

        // public virtual async Task<BaseResponse> DeleteMap(string email, string entLookup, Guid mapID)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var existingMapQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.MapVertexName)
        //             .Has(AmblOnGraphConstants.IDPropertyName, mapID);
                
        //         var existingMaps = await Submit<Map>(existingMapQuery);

        //         var existingMap = existingMaps?.FirstOrDefault();

        //         if (existingMap != null)
        //         {
        //             var deleteQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.MapVertexName)
        //             .Has(AmblOnGraphConstants.IDPropertyName, mapID)
        //             .Drop();

        //             await Submit(deleteQuery);

        //             return new BaseResponse()
        //             {
        //                 Status = Status.Success
        //             };
        //         }
        //         else
        //             return new BaseResponse() { Status = Status.NotLocated.Clone("This map does not exist for this user")};
        //     });
        // }

        public virtual async Task<BaseResponse> DeletePhoto(string email, string entLookup, Guid photoID)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var existingPhoto = await g.V(userId)
                .Out<Owns>()
                .OfType<Photo>()
                .Where(x => x.ID == photoID)
                .FirstOrDefaultAsync();

            if (existingPhoto != null)
            {

                await g.V(userId)
                    .Out<Owns>()
                    .OfType<Photo>()
                    .Where(x => x.ID == photoID)
                    .Drop();

                return new BaseResponse()
                {
                    Status = Status.Success
                };
            }
            else
                return new BaseResponse() { Status = Status.NotLocated.Clone("This photo does not exist for the user")};
        }
        
        // public virtual async Task<BaseResponse> DeleteSharedMap(string email, string entLookup, Guid mapID)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var existingMapQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.SharedMapVertexName)
        //             .Has(AmblOnGraphConstants.IDPropertyName, mapID);
                
        //         var existingMaps = await Submit<SharedMap>(existingMapQuery);

        //         var existingMap = existingMaps?.FirstOrDefault();

        //         if (existingMap != null)
        //         {
        //             var deleteQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.SharedMapVertexName)
        //             .Has(AmblOnGraphConstants.IDPropertyName, mapID)
        //             .Drop();

        //             await Submit(deleteQuery);

        //             return new BaseResponse()
        //             {
        //                 Status = Status.Success
        //             };
        //         }
        //         else
        //             return new BaseResponse() { Status = Status.NotLocated.Clone("This shared map does not exist for this user")};
        //     });
        // }

        public virtual async Task<BaseResponse> DeleteTopList(string email, string entLookup, Guid topListID)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var existingTopList = await g.V(userId)
                .Out<Owns>()
                .OfType<TopList>()
                .Where(x => x.ID == topListID)
                .FirstOrDefaultAsync();

            if (existingTopList != null)
            {
                await g.V(userId)
                    .Out<Owns>()
                    .OfType<TopList>()
                    .Where(x => x.ID == topListID)
                    .Drop();

                return new BaseResponse()
                {
                    Status = Status.Success
                };
            }
            else
                return new BaseResponse() { Status = Status.NotLocated.Clone("This top list does not exist for this user")};
        }

        // public virtual async Task<BaseResponse> DedupLocationsByMap(string email, string entLookup, Guid mapID)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var dedupeGuids = new List<Guid>();

        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         // Load all locations for a mapID
        //         var query = g.V(mapID)
        //             .Out(AmblOnGraphConstants.ContainsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.LocationVertexName);
                    
        //         var locationSet = await Submit<Location>(query);

        //         // Create collections of maps group by lat/lon (sufficient for "equality")
        //         var locationGroups = from l in locationSet.ToList()
        //                             group l by new { Lat = l.Latitude, Lon = l.Longitude} into locGroup
        //                             orderby locGroup.Key.Lat, locGroup.Key.Lon
        //                             select locGroup;

        //         // For each group, take the guids for all but one.
        //         foreach(var locGroup in locationGroups) {
        //             int locSize = locGroup.Count();
        //             if (locSize > 1){
        //                 var locGuids =  locGroup.Select(l => l.ID)
        //                                         .Take(locSize-1)
        //                                         .ToArray();
        //                 //dedupeGuids.AddRange(locGuids);
        //             }
        //         } 
                
        //         // Delete the extraneous locations
        //         var dedupQuery = g.V(mapID)
        //             .Out(AmblOnGraphConstants.ContainsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.LocationVertexName)
        //             .Has("id",  P.Within(dedupeGuids))
        //             .Drop();

        //         var results = await Submit<Location>(dedupQuery);
               
        //         return new BaseResponse()
        //         {
        //                 Status = Status.Success
        //         };
        //     });
        // }
        
        // public virtual async Task<BaseResponse> DeleteMaps(string email, string entLookup, Guid[] mapIDs)
        // {
        //     try {
        //     return await withG(async (client, g) =>
        //     {
        //         var stringGuids = mapIDs.Select(m => m.ToString()).ToArray();

        //         var userId = await ensureAmblOnUser(email, entLookup);
               
        //         var existingMapsQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.MapVertexName)
        //             .Has("id",  P.Within(stringGuids));
   
        //         var existingMaps = await Submit<Map>(existingMapsQuery);

        //         if (existingMaps != null)
        //         {
        //             var deleteQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.MapVertexName)
        //             .Has("id",  P.Within(stringGuids))
        //             .Drop();

        //             await Submit(deleteQuery);

        //             return new BaseResponse()
        //             {
        //                 Status = Status.Success
        //             };
        //         }
        //         else
        //             return new BaseResponse() { Status = Status.NotLocated.Clone("These maps do not exist for this user")};
              
        //     });
        //                   } catch (Exception ex) {
        //            var result = ex.Message;
        //            throw;
        //        }
        // }

        #endregion

        #region Edit
        public virtual async Task<BaseResponse> EditActivity(string email, string entLookup, Activity activity)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var existingActivity = await g.V<AmblOnUser>(userId)
                .Out<Owns>()
                .OfType<Activity>()
                .Where(e => e.ID == activity.ID)
                .FirstOrDefaultAsync();

            if (existingActivity != null)
            {
                // var editQuery = g.V(existingActivity.ID)
                //         .Property("Checked", activity.Checked)
                //         .Property("CreatedDateTime", activity.CreatedDateTime)
                //         .Property("Favorited", activity.Favorited)
                //         .Property("LocationID", activity.LocationID ?? Guid.Empty)
                //         .Property("Notes", activity.Notes ?? "")
                //         .Property("Order", activity.Order)
                //         .Property("Title", activity.Title ?? "")
                //         .Property("TransportIcon", activity.TransportIcon ?? "")
                //         .Property("WidgetIcon", activity.WidgetIcon ?? "");

                var editedActivity = await g.V<Activity>(existingActivity.ID)
                    .Update(activity)
                    .FirstOrDefaultAsync();

                if (existingActivity.LocationID != activity.LocationID)
                {
                    await g.V<Activity>(activity.ID).OutE<OccursAt>().Drop();

                    if (activity.LocationID != null && activity.LocationID != Guid.Empty)
                    {
                        await ensureEdgeRelationship<OccursAt>(activity.ID, activity.LocationID.Value);
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
        }

        public virtual async Task<BaseResponse> EditActivityGroup(string email, string entLookup, ActivityGroup activityGroup)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var existingActivityGroup = await g.V<AmblOnUser>(userId)
                .Out<Owns>()
                .OfType<ActivityGroup>()
                .Where(e => e.ID == activityGroup.ID)
                .FirstOrDefaultAsync();

            if (existingActivityGroup != null)
            {
                // var editQuery = g.V(activityGroup.ID)
                //     .Property("GroupType", activityGroup.GroupType ?? "")
                //     .Property("CreatedDateTime", activityGroup.CreatedDateTime)
                //     .Property("Order", activityGroup.Order)
                //     .Property("Title", activityGroup.Title ?? "");

                var editedActivityGroup = await g.V<ActivityGroup>(existingActivityGroup.ID)
                    .Update(activityGroup)
                    .FirstOrDefaultAsync();

                return new BaseResponse<Guid>()
                {
                    Status = Status.Success
                };
            }
            else
                return new BaseResponse() { 
                    Status = Status.Conflict.Clone("Activity Group not found.")
                };
        }

        // public virtual async Task<BaseResponse> EditAccolade(string email, string entLookup, UserAccolade accolade, Guid locationId)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var lookup = locationId.ToString() + "|" + accolade.Title.Replace(" ", "");

        //         var existingAccoladeQuery = g.V(locationId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.AccoladeVertexName)
        //             .Has(AmblOnGraphConstants.IDPropertyName, accolade.ID);

        //         var existingAccolades = await Submit<Accolade>(existingAccoladeQuery);

        //         var existingAccolade = existingAccolades?.FirstOrDefault();

        //         if (existingAccolades != null)
        //         {
        //             var editQuery = g.V(accolade.ID)
        //                 .Property("Lookup", lookup)
        //                 .Property("Title", accolade.Title)
        //                 .Property("LocationId", accolade.LocationID)
        //                 .Property("Rank", accolade.Rank)
        //                 .Property("Year", accolade.Year);

        //             await Submit(editQuery);

        //             return new BaseResponse()
        //             {
        //                 Status = Status.Success
        //             };
        //         }
        //         else
        //             return new BaseResponse() { Status = Status.NotLocated.Clone("This accolade does not exist for this layer") };
        //     });
        // }
        public virtual async Task<BaseResponse> EditAlbum(string email, string entLookup, Album album)
        {
                var userId = await ensureAmblOnUser(email, entLookup);

                var lookup = userId.ToString() + "|" + album.Title.Replace(" ","");

                var existingAlbum = await g.V<Album>(userId)
                    .Out<Owns>()
                    .OfType<Album>()
                    .Where(e => e.ID == album.ID)
                    .FirstOrDefaultAsync();

                if (existingAlbum != null)
                {
                    // var editQuery = g.V(album.ID)
                    //     .Property("Lookup", lookup)
                    //     .Property("Title", album.Title ?? "");

                    await g.V<Album>(album.ID)
                        .Update(album);

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { Status = Status.NotLocated.Clone("This album does not exist for this user")};
        }
        public virtual async Task<BaseResponse> EditItinerary(string email, string entLookup, Itinerary itinerary)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var lookup = $"{userId.ToString()}|{itinerary.Title.Replace(" ", "_")}";

            var existingItinerary = await g.V<Itinerary>(userId)
                .Out<Owns>()
                .OfType<Itinerary>()
                .Where(x => x.ID == itinerary.ID)
                .FirstOrDefaultAsync();

            if (existingItinerary != null)
            {
                // var editQuery = g.V(itinerary.ID)
                //     .Property("CreatedDateTime", itinerary.CreatedDateTime)
                //     .Property("Title", itinerary.Title ?? "")
                //     .Property("Shared", itinerary.Shared)
                //     .Property("SharedByUsername", itinerary.SharedByUsername ?? "")
                //     .Property("SharedByUserID", itinerary.SharedByUserID)
                //     .Property("Editable", itinerary.Editable)
                //     .Property("Lookup", lookup);

                await g.V<Itinerary>(itinerary.ID)
                    .Update(itinerary);

                return new BaseResponse()
                {
                    Status = Status.Success
                };
            }
            else
                return new BaseResponse<Guid>() { 
                    //Model = existingItinerary.ID.Value,
                    Status = Status.Conflict.Clone("Itinerary not found.")
                };
        }

        // public virtual async Task<BaseResponse> EditLocation(string email, string entLookup, UserLocation location)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var lookup = location.LayerID.ToString() + "|" + location.Latitude.ToString() + "|" + location.Longitude.ToString();

        //         var existingLocationQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.LayerVertexName)
        //             .Has(AmblOnGraphConstants.IDPropertyName, location.LayerID)
        //             .Out(AmblOnGraphConstants.ContainsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.LocationVertexName)
        //             .Has(AmblOnGraphConstants.IDPropertyName, location.ID);
                
        //         var existingLocations = await Submit<Location>(existingLocationQuery);

        //         var existingLocation = existingLocations?.FirstOrDefault();

        //         if (existingLocation != null)
        //         {
        //             var editQuery = g.V(location.ID)
        //                 .Property("Lookup", lookup)
        //                 .Property("Address", location.Address ?? "")
        //                 .Property("Country", location.Country ?? "")
        //                 .Property("Icon", location.Icon ?? "")
        //                 .Property("Instagram", location.Instagram ?? "")
        //                 .Property("IsHidden", location.IsHidden)
        //                 .Property("Latitude", location.Latitude)
        //                 .Property("Longitude", location.Longitude)
        //                 .Property("State", location.State ?? "")
        //                 .Property("Telephone", location.Telephone ?? "")
        //                 .Property("Title", location.Title ?? "")
        //                 .Property("Town", location.Town ?? "")
        //                 .Property("Website", location.Website ?? "")
        //                 .Property("ZipCode", location.ZipCode ?? "");

        //             await Submit(editQuery);

        //             return new BaseResponse()
        //             {
        //                 Status = Status.Success
        //             };
        //         }
        //         else
        //             return new BaseResponse() { Status = Status.NotLocated.Clone("This location does not exist in the user's layer")};
        //     });
        // }

        // public virtual async Task<BaseResponse> EditMap(string email, string entLookup, UserMap map)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var lookup = userId.ToString() + "|" + map.Title.Replace(" ","");

        //         var existingMapQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.MapVertexName)
        //             .Has(AmblOnGraphConstants.IDPropertyName, map.ID);
                
        //         var existingMaps = await Submit<Map>(existingMapQuery);

        //         var existingMap = existingMaps?.FirstOrDefault();

        //         if (existingMap != null)
        //         {
        //             var editQuery = g.V(map.ID)
        //                 .Property(AmblOnGraphConstants.PartitionKeyName, entLookup.ToString())
        //                 .Property("Lookup", lookup)
        //                 .Property("Title", map.Title)
        //                 .Property("Zoom", map.Zoom)
        //                 .Property("Latitude", map.Latitude)
        //                 .Property("Longitude", map.Longitude)
        //                 .Property("Primary", map.Primary)
        //                 .Property("Coordinates", String.Join(",", map.Coordinates))
        //                 .Property("DefaultLayerID", map.DefaultLayerID);

        //             await Submit(editQuery);

        //             if (map.Primary)
        //                 await setPrimaryMap(email, entLookup, (map.ID.HasValue ? map.ID.Value : Guid.Empty));

        //             return new BaseResponse()
        //             {
        //                 Status = Status.Success
        //             };
        //         }
        //         else
        //             return new BaseResponse() { Status = Status.NotLocated.Clone("This map does not exist for this user")};
        //     });
        // }

        public virtual async Task<BaseResponse> EditOrder(string email, string entLookup, string query)
        {
            return await withG(async (client, g) =>
            {
                var results = await SubmitJSON<Itinerary>(query);

                return new BaseResponse();
                
            });
        }

        public virtual async Task<BaseResponse> EditPhoto(string email, string entLookup, Photo photo, Guid albumID)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var lookup = userId.ToString() + "|" + albumID.ToString() + "|" + photo.URL + "|" + photo.LocationID.ToString();

            var existingPhoto = await g.V(userId)
                .Out<Owns>()
                .OfType<Photo>()
                .Where(x => x.ID == photo.ID)
                .FirstOrDefaultAsync();
            
            if (existingPhoto != null)
            {
                await g.V(photo.ID)
                    .Update(photo);

                return new BaseResponse()
                {
                    Status = Status.Success
                };
            }
            else
                return new BaseResponse() { Status = Status.NotLocated.Clone("This photo does not exist for this user")};

        }

        // public virtual async Task<BaseResponse> EditSharedMap(string email, string entLookup, UserMap map)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var lookup = userId.ToString() + "|" + map.Title.Replace(" ","");

        //         var existingMapQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.SharedMapVertexName)
        //             .Has(AmblOnGraphConstants.IDPropertyName, map.ID);
                
        //         var existingMaps = await Submit<SharedMap>(existingMapQuery);

        //         var existingMap = existingMaps?.FirstOrDefault();

        //         if (existingMap != null)
        //         {
        //             var editQuery = g.V(map.ID)
        //                 .Property(AmblOnGraphConstants.PartitionKeyName, entLookup.ToString())
        //                 .Property("Lookup", lookup)
        //                 .Property("Title", map.Title)
        //                 .Property("Deletable", true)
        //                 .Property("Primary", map.Primary)
        //                 .Property("DefaultLayerID", map.DefaultLayerID);

        //             await Submit(editQuery);

        //             if (map.Primary)
        //                 await setPrimaryMap(email, entLookup, (map.ID.HasValue ? map.ID.Value : Guid.Empty));

        //             return new BaseResponse()
        //             {
        //                 Status = Status.Success
        //             };
        //         }
        //         else
        //             return new BaseResponse() { Status = Status.NotLocated.Clone("This shared map does not exist for this user")};
        //     });
        // }

        public virtual async Task<BaseResponse> EditTopList(string email, string entLookup, UserTopList topList)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var lookup = userId.ToString() + "|" + topList.Title.Replace(" ","");

            var existingTopList = await g.V(userId)
                .Out<Owns>()
                .OfType<UserInfo>()
                .Where(x => x.Lookup == lookup)
                .FirstOrDefaultAsync();
            
            if (existingTopList != null)
            {    
                // Update the top list properties

                await g.V(topList.ID)
                    .Update(topList);

                // Delete existing edges                                

                await g.V(existingTopList.ID)
                    .OutE<Contains>()
                    .OfType<Location>()
                    .Drop();

                // Add new edges from ordered list 
                foreach (UserLocation loc in topList.LocationList) {
                    await g.V(existingTopList.ID)
                        .AddE<Contains>()
                        .To(x => x.V(loc.ID))
                        .FirstOrDefaultAsync();
                }

                return new BaseResponse()
                {
                    Status = Status.Success
                };
            }
            else
                return new BaseResponse() { Status = Status.NotLocated.Clone("This top list does not exist for this user")};
        }

        public virtual async Task<BaseResponse> EditUserInfo(string email, string entLookup, UserInfo userInfo)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var lookup = userId.ToString() + "|UserInfo";

            var existingUserInfo = await g.V(userId)
                .Out<Owns>()
                .OfType<UserInfo>()
                .Where(x => x.Lookup == lookup)
                .FirstOrDefaultAsync();

            if (existingUserInfo != null)
            {                               
                await g.V(userInfo.ID)
                    .Update(userInfo);

                return new BaseResponse()
                {
                    Status = Status.Success
                };
            }
            else
                return new BaseResponse() { Status = Status.NotLocated.Clone("This User Info record does not exist for this user")};
        }

        // public virtual async Task<BaseResponse> EditExcludedCurations(string email, string entLookup, ExcludedCurations curations)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         Guid excludedCurationsId;

        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var curationsExistsQuery = g.V(userId)
        //                                 .Out(AmblOnGraphConstants.OwnsEdgeName)
        //                                 .HasLabel("ExcludedCurations");

        //         var existsResult = await Submit<ExcludedCurations>(curationsExistsQuery);
                
        //         var existFirst = existsResult?.FirstOrDefault();

        //         if (existFirst == null) {
        //             var createQuery = g.AddV(AmblOnGraphConstants.ExcludedCurationsName)
        //                 .Property(AmblOnGraphConstants.PartitionKeyName, entLookup.ToString())
        //                 .Property("LocationIDs", curations.LocationIDs);

        //             var createCurationsResults = await Submit<ExcludedCurations>(createQuery);

        //             var createdCurations = createCurationsResults?.FirstOrDefault();

        //             var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdCurations.ID));

        //             await Submit(userEdgeQuery);

        //             excludedCurationsId = createdCurations.ID;
        //         } else {
        //             var updateQuery = g.V(existFirst.ID)
        //                 .Property("LocationIDs", curations.LocationIDs);

        //             await Submit(updateQuery);

        //             excludedCurationsId = existFirst.ID;
        //         }

        //         return new BaseResponse()
        //         {
        //             Status = Status.Success
        //         };
        //     });
        // }
        
        public virtual async Task<BaseResponse> QuickEditActivity(Activity activity)
        {

            var editQuery = await g.V(activity.ID)
                .Update(activity);
                
                // ("Checked", activity.Checked)
                // .Property("Editable", activity.Editable)
                // .Property("Favorited", activity.Favorited)
                // .Property("Notes", activity.Notes ?? "")
                // .Property("Order", activity.Order.ToString() ?? "")
                // .Property("Title", activity.Title ?? "")
                // .Property("TransportIcon", activity.TransportIcon ?? "")
                // .Property("WidgetIcon", activity.WidgetIcon ?? "");

            return new BaseResponse()
            {
                Status = Status.Success
            };
        }        
        #endregion 

        #region List
        public virtual async Task<BaseResponse<UserInfo>> GetUserInfo(string email, string entLookup)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var lookup = userId.ToString() + "|UserInfo";

            var existingUserInfo = await g.V(userId)
                .Out<Owns>()
                .OfType<UserInfo>()
                .Where(x => x.Lookup == lookup)
                .FirstOrDefaultAsync();

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
        }
        // public virtual async Task<List<Activity>> ListActivities(string email, string entLookup, Guid activityGroupId)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var query = g.V(activityGroupId)
        //             .Out(AmblOnGraphConstants.ContainsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.ActivityVertexName);

        //         var results = await Submit<Activity>(query);

        //         results.ToList().ForEach(
        //             (activity) =>
        //             {
        //                 var locationId = getActivityLocationID(activity.ID).GetAwaiter().GetResult();
        //                 activity.LocationID = locationId;
        //             });

        //         return results.ToList();
        //     });
        // }

        // public virtual async Task<List<ActivityGroup>> ListActivityGroups(string email, string entLookup, Itinerary itinerary)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         // Check to see if the itinerary is shared. If shared, switch the "Out" part of the query to "CanView" instead of "Owns"
        //         var outVertexName = "";

        //         if(itinerary.Shared){
        //             outVertexName = "CanView";               
        //         }
        //         else{
        //             outVertexName = "Owns";
        //         }

        //         var query = g.V(userId)
        //             .Out(outVertexName)
        //             .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
        //             .Has(AmblOnGraphConstants.IDPropertyName, itinerary.ID)
        //             .Out(AmblOnGraphConstants.ContainsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.ActivityGroupVertexName);

        //         var results = await Submit<ActivityGroup>(query);

        //         return results.ToList();
        //     });
        // }

        // public virtual async Task<List<Accolade>> ListAccolades(string email, string entLookup, Guid locationId)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var query = g.V(locationId)
        //             .Out(AmblOnGraphConstants.ContainsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.AccoladeVertexName);

        //         var results = await Submit<Accolade>(query);

        //         return results.ToList();
        //     });
        // }

        public virtual async Task<List<Album>> ListAlbums(string email, string entLookup)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(email, entLookup);

                var query = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.AlbumVertexName)
                    .Project<UserAlbum>("id", "PartitionKey", "Label", "Lookup", "Title", "Photos")
                    .By("id").By("PartitionKey").By("label").By("Lookup").By("Title")
                    .By(__.Out("Contains").HasLabel(AmblOnGraphConstants.PhotoVertexName).Project<Photo>("id", "PartitionKey", "Label", "Lookup", "Caption", "URL")
                    .By("id").By("PartitionKey").By("label").By("Lookup").By("Caption").By("URL")
                    .Fold());

                var results = await SubmitJSON<UserAlbum>(query);

                return results.ToList();
            });
        }

        // public virtual async Task<List<Itinerary>> ListItineraries(string email, string entLookup)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var query = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.ItineraryVertexName);

        //         var ownedResults = await Submit<Itinerary>(query);

        //         var ownedList = ownedResults.ToList();

        //         ownedList.ForEach(
        //             (owned) =>
        //             {
        //                 owned.Shared = false;
        //                 owned.SharedByUserID = Guid.Empty;
        //                 owned.SharedByUsername = "";
        //                 owned.Editable = true;
        //             });

        //         var sharedQuery = g.V(userId)
        //               .Out(AmblOnGraphConstants.CanViewEdgeName)
        //               .HasLabel(AmblOnGraphConstants.ItineraryVertexName);

        //         var sharedResults = await Submit<Itinerary>(sharedQuery);

        //         var sharedList = sharedResults.ToList();

        //         sharedList.ForEach(
        //             (shared) =>
        //             {
        //                 shared.Shared = true;
        //                 shared.Editable = false;

        //                 var userQuery = g.V(shared.ID)
        //                       .In(AmblOnGraphConstants.OwnsEdgeName)
        //                       .HasLabel(AmblOnGraphConstants.AmblOnUserVertexName);
                            
        //                 var userResults = Submit<AmblOnUser>(userQuery).GetAwaiter().GetResult();

        //                 var user = userResults?.FirstOrDefault();

        //                 var userInfoQuery = g.V(user.ID)
        //                         .Out(AmblOnGraphConstants.OwnsEdgeName)
        //                         .HasLabel(AmblOnGraphConstants.UserInfoVertexName);

        //                 var userInfoResults = Submit<UserInfo>(userInfoQuery).GetAwaiter().GetResult();

        //                 var userInfo = userInfoResults?.FirstOrDefault();

        //                 if (userInfo != null)
        //                 {
        //                     shared.SharedByUserID = user.ID;
        //                     shared.SharedByUsername = userInfo.FirstName + " " + userInfo.LastName;
        //                 }
        //                 else{
        //                     shared.SharedByUserID = user.ID;
        //                     shared.SharedByUsername = user.Email;
        //                 }
        //             });

        //         var results = new List<Itinerary>();

        //         results.AddRange(ownedList);
        //         results.AddRange(sharedList);
                
        //         return results.ToList();
        //     });
        // }
        		public virtual async Task<T> SubmitJSONFirst<T>(string script)
		        {
                    var value = await SubmitJSON<T>(script);

                    return value.FirstOrDefault();
                }
		public virtual async Task<List<T>> SubmitJSON<T>(string script)
		{
			return await withClient(async (client) =>
			{
				ResultSet<dynamic> res = await client.SubmitAsync<dynamic>(script);

				var vals = res?.Select<dynamic, T>(ta =>
				{
                    return ((object)ta).JSONConvert<T>();
					
				})?.ToList();

				return vals;
			});
		}

        public virtual async Task<List<T>> SubmitJSON<T>(ITraversal traversal)
		{
			return await SubmitJSON<T>(traversal.ToGremlinQuery());
		}

        public virtual async Task<T> SubmitJSONFirst<T>(ITraversal traversal)
		{
			return await SubmitJSONFirst<T>(traversal.ToGremlinQuery());
		}
        
        public virtual async Task<List<Itinerary>> ListItineraries(string email, string entLookup)
        {
                var userId = await ensureAmblOnUser(email, entLookup);

                var ownedList = await g.V(userId)
                    .Out<Owns>()
                    .OfType<Itinerary>()
                    .Project<Itinerary>(x => x.ToDynamic().By(itinerary => itinerary.ID))


                var query = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Project<Itinerary>("id", "PartitionKey", "Label", "Lookup", "Shared", "SharedByUsername", "SharedByUserID", "Title", "Editable", "CreatedDateTime", "ActivityGroups")
                    .By("id").By("PartitionKey").By("label").By("Lookup").By("Shared").By("SharedByUsername").By("SharedByUserID").By("Title").By("Editable").By("CreatedDateTime")
                    .By(__.Out("Contains").HasLabel(AmblOnGraphConstants.ActivityGroupVertexName).Project<ActivityGroup>("id", "PartitionKey", "Label", "Lookup", "GroupType", "Order", "Checked", "Title", "CreatedDateTime", "Activities")
                    .By("id").By("PartitionKey").By("label").By("Lookup").By("GroupType").By("Order").By("Checked").By("Title").By("CreatedDateTime")
                    .By(__.Out("Contains").HasLabel("Activity").Project<Activity>("id", "PartitionKey", "Label", "Lookup", "Favorited", "Order", "Notes", "TransportIcon", "WidgetIcon", "LocationID", "Checked", "Title", "CreatedDateTime")
                    .By("id").By("PartitionKey").By("label").By("Lookup").By("Favorited").By("Order").By("Notes").By("TransportIcon").By("WidgetIcon").By("LocationID").By("Checked").By("Title").By("CreatedDateTime")
                    .Fold()).Fold());

                var ownedResults = await SubmitJSON<Itinerary>(query);

                var ownedList = ownedResults.ToList();

                ownedList.ForEach(
                    (owned) =>
                    {
                        var AGList = owned;
                        owned.Shared = false;
                        owned.SharedByUserID = Guid.Empty;
                        owned.SharedByUsername = "";
                        owned.Editable = true;
                    });

                var sharedQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.CanViewEdgeName)
                    .HasLabel(AmblOnGraphConstants.ItineraryVertexName)
                    .Project<Itinerary>("id", "PartitionKey", "Label", "Lookup", "Shared", "SharedByUsername", "SharedByUserID", "Title", "Editable", "CreatedDateTime", "ActivityGroups")
                    .By("id").By("PartitionKey").By("label").By("Lookup").By("Shared").By("SharedByUsername").By("SharedByUserID").By("Title").By("Editable").By("CreatedDateTime")
                    .By(__.Out("Contains").HasLabel(AmblOnGraphConstants.ActivityGroupVertexName).Project<ActivityGroup>("id", "PartitionKey", "Label", "Lookup", "GroupType", "Order", "Checked", "Title", "CreatedDateTime", "Activities")
                    .By("id").By("PartitionKey").By("label").By("Lookup").By("GroupType").By("Order").By("Checked").By("Title").By("CreatedDateTime")
                    .By(__.Out("Contains").HasLabel("Activity").Project<Activity>("id", "PartitionKey", "Label", "Lookup", "Favorited", "Order", "Notes", "TransportIcon", "WidgetIcon", "LocationID", "Checked", "Title", "CreatedDateTime")
                    .By("id").By("PartitionKey").By("label").By("Lookup").By("Favorited").By("Order").By("Notes").By("TransportIcon").By("WidgetIcon").By("LocationID").By("Checked").By("Title").By("CreatedDateTime")
                    .Fold()).Fold());

                var sharedResults = await SubmitJSON<Itinerary>(sharedQuery);

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
        }

        // public virtual async Task<List<Layer>> ListLayers(string email, string entLookup)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var query = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.LayerVertexName);

        //         var results = await Submit<Layer>(query);

        //         return results.ToList();
        //     });
        // }

        public virtual async Task<List<Location>> ListTopListLocations(string email, string entLookup, Guid topListID)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var topListLocations = await g.V(topListID)
                .Out<Contains>()
                .OfType<Location>()
                .ToListAsync();

            return topListLocations;
        }
        // public virtual async Task<List<Location>> ListLocations(string email, string entLookup, Guid layerID)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var query = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.LayerVertexName)
        //             .Has(AmblOnGraphConstants.IDPropertyName, layerID)
        //             .Out(AmblOnGraphConstants.ContainsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.LocationVertexName);

        //         var query2 = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.LocationVertexName);

        //         var results = await Submit<Location>(query);               

        //         if (results.ToList().Count == 0)
        //         {
        //             query = g.V(userId)
        //                 .Out(AmblOnGraphConstants.OwnsEdgeName)
        //                 .HasLabel(AmblOnGraphConstants.SharedLayerVertexName)
        //                 .Has(AmblOnGraphConstants.IDPropertyName, layerID)
        //                 .Out(AmblOnGraphConstants.InheritsEdgeName)
        //                 .HasLabel(AmblOnGraphConstants.LayerVertexName)
        //                 .Out(AmblOnGraphConstants.ContainsEdgeName)
        //                 .HasLabel(AmblOnGraphConstants.LocationVertexName);

        //             results = await Submit<Location>(query);                  
        //         }
        //         //Query to return locations directly associated with AmblOnUser account
        //         var otherResults = await Submit<Location>(query2);

        //         var totalResults = results.ToList();

        //         totalResults.AddRange(otherResults.ToList());

        //         return totalResults;
        //     });
        // }

        public virtual async Task<List<Location>> PopulateAllLocations(string email, string entLookup)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            // var query = g.V(userId)
            //     .Out(AmblOnGraphConstants.OwnsEdgeName)
            //     .HasLabel(AmblOnGraphConstants.LayerVertexName)
            //     .Has(AmblOnGraphConstants.IDPropertyName, layerID)
            //     .Out(AmblOnGraphConstants.ContainsEdgeName)
            //     .HasLabel(AmblOnGraphConstants.LocationVertexName);
            
            var locations = await g.V<Location>(userId)
                .Out<Owns>()
                .OfType<Activity>()
                .Out<OccursAt>()
                .OfType<Location>()
                .Dedup()
                .ToListAsync();

            // var query = g.V(userId)
            //     .Out(AmblOnGraphConstants.OwnsEdgeName)
            //     .HasLabel(AmblOnGraphConstants.ActivityVertexName)
            //     .Out(AmblOnGraphConstants.OccursAtEdgeName)
            //     .Dedup();                                       

            // var query2 = g.V(userId)
            //     .Out(AmblOnGraphConstants.OwnsEdgeName)
            //     .HasLabel(AmblOnGraphConstants.LocationVertexName);              

            // if (results.ToList().Count == 0)
            // {
            //     query = g.V(userId)
            //         .Out(AmblOnGraphConstants.OwnsEdgeName)
            //         .HasLabel(AmblOnGraphConstants.SharedLayerVertexName)
            //         .Has(AmblOnGraphConstants.IDPropertyName, layerID)
            //         .Out(AmblOnGraphConstants.InheritsEdgeName)
            //         .HasLabel(AmblOnGraphConstants.LayerVertexName)
            //         .Out(AmblOnGraphConstants.ContainsEdgeName)
            //         .HasLabel(AmblOnGraphConstants.LocationVertexName);

            //     results = await Submit<Location>(query);                  
            // }
            //Query to return locations directly associated with AmblOnUser account
            // var otherResults = await Submit<Location>(query2);

            // var totalResults = results.ToList();

            // totalResults.AddRange(otherResults.ToList());

            return locations;
        }

        // public virtual async Task<List<Location>> ListLocations(string email, string entLookup)
        // {
        //     var userId = await ensureAmblOnUser(email, entLookup);

        //     // var query = g.V(userId)
        //     //     .Out(AmblOnGraphConstants.OwnsEdgeName)
        //     //     .HasLabel(AmblOnGraphConstants.LayerVertexName)
        //     //     .Has(AmblOnGraphConstants.IDPropertyName, layerID)
        //     //     .Out(AmblOnGraphConstants.ContainsEdgeName)
        //     //     .HasLabel(AmblOnGraphConstants.LocationVertexName);

        //     var query2 = g.V(userId)
        //         .Out(AmblOnGraphConstants.OwnsEdgeName)
        //         .HasLabel(AmblOnGraphConstants.LocationVertexName);

        //     // var results = await Submit<Location>(query);               

        //     // if (results.ToList().Count == 0)
        //     // {
        //     //     query = g.V(userId)
        //     //         .Out(AmblOnGraphConstants.OwnsEdgeName)
        //     //         .HasLabel(AmblOnGraphConstants.SharedLayerVertexName)
        //     //         .Has(AmblOnGraphConstants.IDPropertyName, layerID)
        //     //         .Out(AmblOnGraphConstants.InheritsEdgeName)
        //     //         .HasLabel(AmblOnGraphConstants.LayerVertexName)
        //     //         .Out(AmblOnGraphConstants.ContainsEdgeName)
        //     //         .HasLabel(AmblOnGraphConstants.LocationVertexName);

        //     //     results = await Submit<Location>(query);                  
        //     // }
        //     //Query to return locations directly associated with AmblOnUser account
        //     var otherResults = await Submit<Location>(query2);

        //     // var totalResults = results.ToList();

        //     // totalResults.AddRange(otherResults.ToList());

        //     return otherResults.ToList();
        // }

        // public virtual async Task<List<Map>> ListMaps(string email, string entLookup)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var query = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.MapVertexName);

        //         var results = await Submit<Map>(query);

        //         return results.ToList();
        //     });
        // }

        public virtual async Task<List<Photo>> ListPhotos(string email, string entLookup, Guid albumID)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var photos = await g.V(userId)
                .Out<Owns>()
                .OfType<Album>()
                .Where(x => x.ID == albumID)
                .Out<Contains>()
                .OfType<Photo>();

            // var query = g.V(userId)
            //     .Out(AmblOnGraphConstants.OwnsEdgeName)
            //     .HasLabel(AmblOnGraphConstants.AlbumVertexName)
            //     .Has(AmblOnGraphConstants.IDPropertyName, albumID)
            //     .Out(AmblOnGraphConstants.ContainsEdgeName)
            //     .HasLabel(AmblOnGraphConstants.PhotoVertexName);

            // var results = await Submit<Photo>(query);

            // results.ToList().ForEach(
            //     (photo) =>
            //     {
            //         var locationId = getPhotoLocationID(userId, photo.ID).GetAwaiter().GetResult();
            //         photo.LocationID = locationId;
            //     });

            return photos.ToList();
        }

        // public virtual async Task<List<Tuple<SharedLayer, Layer>>> ListSharedLayers(string email, string entLookup)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var query = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.SharedLayerVertexName);

        //         var results = await Submit<SharedLayer>(query);

        //         var returnValues = new List<Tuple<SharedLayer, Layer>>();

        //         results.ToList().ForEach(
        //             (sharedLayer) =>
        //             {
        //                 var layerQuery = g.V(userId)
        //                     .Out(AmblOnGraphConstants.OwnsEdgeName)
        //                     .HasLabel(AmblOnGraphConstants.SharedLayerVertexName)
        //                     .Has(AmblOnGraphConstants.IDPropertyName, sharedLayer.ID)
        //                     .Out(AmblOnGraphConstants.InheritsEdgeName)
        //                     .HasLabel(AmblOnGraphConstants.LayerVertexName);
                            
        //                 var layerResults = Submit<Layer>(layerQuery).GetAwaiter().GetResult();

        //                 var layer = layerResults.FirstOrDefault();

        //                 if (layer != null)
        //                     returnValues.Add(Tuple.Create<SharedLayer, Layer>(sharedLayer, layer));
        //             });

        //         return returnValues;
        //     });
        // }

        // public virtual async Task<List<Tuple<SharedMap, Map>>> ListSharedMaps(string email, string entLookup)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var query = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.SharedMapVertexName);

        //         var results = await Submit<SharedMap>(query);

        //         var returnValues = new List<Tuple<SharedMap, Map>>();

        //         results.ToList().ForEach(
        //             (sharedMap) =>
        //             {
        //                 var mapQuery = g.V(userId)
        //                     .Out(AmblOnGraphConstants.OwnsEdgeName)
        //                     .HasLabel(AmblOnGraphConstants.SharedMapVertexName)
        //                     .Has(AmblOnGraphConstants.IDPropertyName, sharedMap.ID)
        //                     .Out(AmblOnGraphConstants.InheritsEdgeName)
        //                     .HasLabel(AmblOnGraphConstants.MapVertexName);
                            
        //                 var mapResults = Submit<Map>(mapQuery).GetAwaiter().GetResult();

        //                 var map = mapResults.FirstOrDefault();

        //                 if (map != null)
        //                     returnValues.Add(Tuple.Create<SharedMap, Map>(sharedMap, map));
        //             });

        //         return returnValues;
        //     });
        // }
        
        public virtual async Task<List<UserTopList>> ListTopLists(string email, string entLookup)
        {
                var userId = await ensureAmblOnUser(email, entLookup);

                var topLists = await g.V(userId)
                    .Out<Owns>()
                    .OfType<TopList>();

                return topLists.ToList();

        }

        // public virtual async Task<ExcludedCurations> ListExcludedCurations(string email, string entLookup)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var query = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.ExcludedCurationsName);

        //         var results = await Submit<ExcludedCurations>(query);

        //         return results?.FirstOrDefault();
        //     });

        // }
        #endregion

        public virtual async Task<BaseResponse> ShareItinerary(string email, string entLookup, Itinerary itinerary, string shareWithUsername)
        {
            var userId = await ensureAmblOnUser(email, entLookup);
            var shareUserId = await ensureAmblOnUser(shareWithUsername, entLookup);

            if(userId.ToString() == shareUserId.ToString()){
                return new BaseResponse() { 
                Status = Status.Conflict.Clone("Journey can't be shared with yourself!")
                };
            };

            var existingItinerary = await g.V<Itinerary>(shareUserId)
                .Out<CanView>()
                .OfType<Itinerary>()
                .Where(x => x.ID == itinerary.ID)
                .FirstOrDefaultAsync();

            if (existingItinerary == null)
            {
                var originalItinerary = await g.V<Itinerary>(userId)
                    .Out<Owns>()
                    .OfType<Itinerary>()
                    .Where(x => x.ID == itinerary.ID)
                    .FirstOrDefaultAsync();

                if (originalItinerary != null)
                {
                    await g.V(userId)
                        .AddE<CanView>()
                        .To(x => x.V(originalItinerary.ID))
                        .FirstOrDefaultAsync();

                    return new BaseResponse()
                    {
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse() { 
                    Status = Status.Conflict.Clone("Journey not found.")
                };
            }
            else
                return new BaseResponse() { 
                    Status = Status.Conflict.Clone("Journey is already shared with this user.")
                };
        }

        public virtual async Task<BaseResponse> UnshareItinerary(string email, string entLookup, Itinerary itinerary, string shareWithUsername)
        {
            var userId = await ensureAmblOnUser(email, entLookup);

            var existingItinerary = await g.V<Itinerary>(userId)
                .Out<CanView>()
                .OfType<Itinerary>()
                .Where(x => x.ID == itinerary.ID)
                .FirstOrDefaultAsync();

            if (existingItinerary != null)
            {
                var originalItinerary = await g.V<Itinerary>(itinerary.ID)
                    .FirstOrDefaultAsync();

                if (originalItinerary != null)
                {
                    await g.V(userId)
                        .AddE<CanView>()
                        .To(x => x.V(originalItinerary.ID))
                        .Drop()
                        .FirstOrDefaultAsync();

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
        }
        #endregion

        #region Helpers
        //Takes in a location, determines if the location exists in the Graph. If it does, update it. If it does not, create the location vertex and edge relationship
        public virtual async Task<Location> ensureLocation(string email, string entLookup, Location location)
        {
            //var userId = await ensureAmblOnUser(email, entLookup);

            string lookup = location.Latitude.ToString() + "|" + location.Longitude.ToString();
                                        
            var existingLocation = await g.V<Location>()
                .Where(x => x.Lookup == lookup)
                .FirstOrDefaultAsync();

            if (existingLocation != null){
                await g.V<Location>(existingLocation.ID)
                    .Update(location);
                // var editQuery = g.V(existingLocation.ID)
                // .Property("Address", location.Address ?? "")
                // .Property("Country", location.Country ?? "")
                // .Property("Icon", location.Icon ?? "")
                // .Property("Instagram", location.Instagram ?? "")
                // .Property("IsHidden", location.IsHidden ?? "")
                // .Property("Latitude", location.Latitude)
                // .Property("Longitude", location.Longitude)
                // .Property("State", location.State ?? "")
                // .Property("Telephone", location.Telephone ?? "")
                // .Property("Title", location.Title ?? "")
                // .Property("Town", location.Town ?? "")
                // .Property("Website", location.Website ?? "")
                // .Property("ZipCode", location.ZipCode ?? "");

                return existingLocation;
            }

            else{
                var createdLocation = await g.AddV<Location>(new Location(){
                    Lookup = lookup ?? "",
                    PartitionKey = Convert.ToInt32(location.Latitude).ToString() + Convert.ToInt32(location.Longitude).ToString(),
                    Address =location.Address ?? "",
                    Country = location.Country ?? "",
                    GoogleLocationName = location.GoogleLocationName ?? "",
                    Icon =  location.Icon ?? "",
                    Instagram = location.Instagram ?? "",
                    IsHidden = location.IsHidden ?? "",
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    State = location.State ?? "",
                    Telephone = location.Telephone ?? "",
                    Title = location.Title ?? "",
                    Town = location.Town ?? "",
                    Website = location.Website ?? "",
                    ZipCode = location.ZipCode ?? ""
                })
                .FirstOrDefaultAsync();

                // var createQuery = g.AddV(AmblOnGraphConstants.LocationVertexName)
                // .Property(AmblOnGraphConstants.PartitionKeyName, Convert.ToInt32(location.Latitude).ToString() + Convert.ToInt32(location.Longitude).ToString())
                // .Property("Lookup", lookup ?? "")
                // .Property("Address", location.Address ?? "")                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      
                // .Property("Country", location.Country ?? "")
                // .Property("GoogleLocationName", location.GoogleLocationName ?? "")
                // .Property("Icon", location.Icon ?? "")
                // .Property("Instagram", location.Instagram ?? "")
                // .Property("IsHidden", location.IsHidden ?? "")
                // .Property("Latitude", location.Latitude)
                // .Property("Longitude", location.Longitude)
                // .Property("State", location.State ?? "")
                // .Property("Telephone", location.Telephone ?? "")
                // .Property("Title", location.Title ?? "")
                // .Property("Town", location.Town ?? "")
                // .Property("Website", location.Website ?? "")
                // .Property("ZipCode", location.ZipCode ?? "");

                //acLoc.Activity.LocationID = createdLocation.ID;

                //var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdLocation.ID));

                //await Submit(userEdgeQuery);

                return createdLocation;                 
            } 
        }

        public virtual async Task<Location> ensureLocation(string email, string entLookup, Guid? locationID)
        {                                    
            var existingLocation = await g.V<Location>()
                .Where(x => x.ID == locationID)
                .FirstOrDefaultAsync();
            
            if(existingLocation != null)
                return existingLocation;
            else
                return null;           
        }       

        public virtual async Task<Guid> ensureAmblOnUser(string email, string entLookup)
        {
            var partKey = email?.Split('@')[1];

            var existingUser = await g.V<AmblOnUser>()
                .Where(e => e.PartitionKey == partKey)
                .Where(e => e.Email == email)
                .FirstOrDefaultAsync();

            if (existingUser == null)
            {
                existingUser = await setupNewUser(email, entLookup);
            }

            return existingUser.ID;
        }

        public virtual async Task<Guid> getActivityLocationID(Guid? activityId)
        {
            var location = await g.V<Location>()
                .Out<Owns>()
                .OfType<Activity>()
                .Where(x => x.ID == (activityId.HasValue ? activityId.Value : Guid.Empty))
                .Out<OccursAt>()
                .OfType<Location>()
                .FirstOrDefaultAsync();

            if (location != null)
                return location.ID;
            else
                return Guid.Empty;           
        }

        // public virtual async Task<Guid> getPhotoLocationID(Guid userId, Guid photoId)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var query = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.PhotoVertexName)
        //             .Has(AmblOnGraphConstants.IDPropertyName, photoId)
        //             .Out(AmblOnGraphConstants.OccursAtEdgeName)
        //             .HasLabel(AmblOnGraphConstants.LocationVertexName);

        //         var results = await Submit<Location>(query);

        //         var location = results.FirstOrDefault();

        //         if (location != null)
        //             return location.ID.Value;
        //         else
        //             return Guid.Empty;
        //     });
        // }

        // public virtual async Task<Status> setPrimaryMap(string email, string entLookup, Guid mapId)
        // {
        //     return await withG(async (client, g) =>
        //     {
        //         var userId = await ensureAmblOnUser(email, entLookup);

        //         var oldMapsQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.MapVertexName)
        //             .Has("Primary", true)
        //             .Property("Primary", false);

        //         await Submit(oldMapsQuery);

        //         var oldSharedMapsQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.SharedMapVertexName)
        //             .Has("Primary", true)
        //             .Property("Primary", false);

        //         await Submit(oldSharedMapsQuery);

        //         var existingMapQuery = g.V(userId)
        //             .Out(AmblOnGraphConstants.OwnsEdgeName)
        //             .HasLabel(AmblOnGraphConstants.MapVertexName)
        //             .Has("id", mapId);

        //         var existingMaps = await Submit<Map>(existingMapQuery);

        //         var existingMap = existingMaps?.FirstOrDefault();

        //         if (existingMap != null)
        //         {
        //             existingMapQuery = g.V(userId)
        //                 .Out(AmblOnGraphConstants.OwnsEdgeName)
        //                 .HasLabel(AmblOnGraphConstants.MapVertexName)
        //                 .Has("id", mapId)
        //                 .Property("Primary", true);

        //             await Submit(existingMapQuery);
        //         }
        //         else
        //         {
        //             var existingSharedMapQuery =  g.V(userId)
        //                 .Out(AmblOnGraphConstants.OwnsEdgeName)
        //                 .HasLabel(AmblOnGraphConstants.SharedMapVertexName)
        //                 .Has("id", mapId);

        //             var existingSharedMaps = await Submit<Map>(existingSharedMapQuery);

        //             var existingSharedMap = existingSharedMaps?.FirstOrDefault();

        //              if (existingSharedMap != null)
        //             {
        //                 existingSharedMapQuery = g.V(userId)
        //                     .Out(AmblOnGraphConstants.OwnsEdgeName)
        //                     .HasLabel(AmblOnGraphConstants.SharedMapVertexName)
        //                     .Has("id", mapId)
        //                     .Property("Primary", true);

        //                 await Submit(existingSharedMapQuery);
        //             }
        //         }

        //         return Status.Success;
        //     });
        // }

        public virtual async Task<AmblOnUser> setupNewUser(string email, string entLookup)
        {
            var partKey = email?.Split('@')[1];

            var user = await g.AddV<AmblOnUser>(new AmblOnUser(){
                PartitionKey = partKey, 
                Email = email
            })
            .FirstOrDefaultAsync();

            // await AddLayer(email, entLookup, new UserLayer()
            // {
            //     Title = "User"
            // });

            // var sharedMapQuery = g.V()
            //         .HasLabel(AmblOnGraphConstants.AmblOnUserVertexName)
            //         .Has("Email", AmblOnGraphConstants.DefaultUserEmail)
            //         .Out(AmblOnGraphConstants.OwnsEdgeName)
            //         .HasLabel(AmblOnGraphConstants.MapVertexName)
            //         .Has(AmblOnGraphConstants.LookupPropertyName, AmblOnGraphConstants.DefaultUserID + "|DefaultMap");

            // var sharedMapResults = await Submit<Map>(sharedMapQuery);

            // var sharedMapResult = sharedMapResults.Any() ? sharedMapResults.FirstOrDefault().ID : Guid.Empty;

            // var sharedLayerQuery = g.V()
            //         .HasLabel(AmblOnGraphConstants.AmblOnUserVertexName)
            //         .Has("Email", AmblOnGraphConstants.DefaultUserEmail)
            //         .Out(AmblOnGraphConstants.OwnsEdgeName)
            //         .HasLabel(AmblOnGraphConstants.LayerVertexName)
            //         .Has(AmblOnGraphConstants.LookupPropertyName, AmblOnGraphConstants.DefaultUserID + "|DefaultLayer");

            // var sharedLayerResults = await Submit<Map>(sharedLayerQuery);

            // var sharedLayerResult = sharedLayerResults.Any() ? sharedLayerResults.FirstOrDefault().ID : Guid.Empty;

            // await AddSharedMap(email, entLookup, new SharedMap()
            // {
            //     Title = "Global",
            //     Deletable = false,
            //     DefaultLayerID = sharedLayerResult
            // }, false, sharedMapResult);

            // await AddSharedLayer(email, entLookup, new UserLayer()
            // {
            //     Title = "Curated",
            //     Deletable = false,
            // }, false, sharedLayerResult, sharedMapResult);

            return user;
        }
        #endregion
    }
}