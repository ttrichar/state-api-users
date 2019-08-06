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
        public virtual async Task<BaseResponse<Guid>> AddLocation(Location location, string consumerId = "")
        {
            return await withG(async (client, g) =>
            {
                var existingLocationQuery = g.V()
                    .HasLabel(AmblOnGraphConstants.LocationVertexName)
                    .Has("Latitude", location.Latitude)
                    .Has("Longitude", location.Longitude);
                
                var existingLocations = await Submit<Location>(existingLocationQuery);

                var existingLocation = existingLocations?.FirstOrDefault();

                if (existingLocation == null)
                {
                    var createQuery = g.AddV(AmblOnGraphConstants.LocationVertexName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, Convert.ToInt32(location.Latitude).ToString() + Convert.ToInt32(location.Longitude).ToString())
                        .Property("Address", location.Address)
                        .Property("Country", location.Title)
                        .Property("Icon", location.Icon)
                        .Property("Instagram", location.Instagram)
                        .Property("Latitude", location.Latitude)
                        .Property("Longitude", location.Longitude)
                        .Property("State", location.State)
                        .Property("Telephone", location.Telephone)
                        .Property("Title", location.Title)
                        .Property("Town", location.Town)
                        .Property("Website", location.Website)
                        .Property("ZipCode", location.ZipCode);

                    var createLocationResults = await Submit<Map>(createQuery);

                    var createdLocation = createLocationResults?.FirstOrDefault();

                    if (consumerId.Contains("@"))
                    {
                        var userId = ensureAmblOnUser(g, consumerId);

                        var userEdgeQuery = g.V(userId).AddE(AmblOnGraphConstants.ConsumesEdgeName).To(g.V(createdLocation.ID));

                        await Submit(userEdgeQuery);
                    }
                    else if (!consumerId.IsNullOrEmpty())
                    {
                        var consumerGuid = Guid.Parse(consumerId);
                        
                        var locationEdgeQuery =  g.V(consumerGuid).AddE(AmblOnGraphConstants.ConsumesEdgeName).To(g.V(createdLocation.ID));

                        await Submit(locationEdgeQuery);
                    }

                    return new BaseResponse<Guid>()
                    {
                        Model = createdLocation.ID,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { Status = Status.Conflict.Clone("A location by that lat/long already exists.")};
            });
        }

        public virtual async Task<BaseResponse<Guid>> AddMap(string email, Map map, List<Guid> locationIds)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email);

                var existingMapQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.MapVertexName)
                    .Has("Lookup", map.Lookup);

                var existingMaps = await Submit<Map>(existingMapQuery);

                var existingMap = existingMaps?.FirstOrDefault();

                if (existingMap == null)
                {
                    var createQuery = g.AddV(AmblOnGraphConstants.MapVertexName)
                        .Property(AmblOnGraphConstants.PartitionKeyName, userId.ToString())
                        .Property("Lookup", map.Lookup)
                        .Property("Title", map.Title)
                        .Property("Zoom", map.Zoom)
                        .Property("Latitude", map.Latitude)
                        .Property("Longitude", map.Longitude)
                        .Property("Primary", true);

                    var createMapResults = await Submit<Map>(createQuery);

                    var createdMap = createMapResults?.FirstOrDefault();

                    await SetPrimaryMap(email, createdMap.ID);

                    var mapEdgeQueries = new[] {
                        g.V(userId).AddE(AmblOnGraphConstants.ConsumesEdgeName).To(g.V(createdMap.ID)),
                        g.V(userId).AddE(AmblOnGraphConstants.OwnsEdgeName).To(g.V(createdMap.ID)),
                        g.V(userId).AddE(AmblOnGraphConstants.ManagesEdgeName).To(g.V(createdMap.ID))
                    };

                    foreach (var edgeQuery in mapEdgeQueries)
                        await Submit(edgeQuery);

                    mapEdgeQueries = locationIds.Select(locId =>
                    {
                        return g.V(createdMap.ID).AddE(AmblOnGraphConstants.ConsumesEdgeName).To(g.V(locId));
                    }).ToArray();

                    foreach (var edgeQuery in mapEdgeQueries)
                        await Submit(edgeQuery);

                    return new BaseResponse<Guid>()
                    {
                        Model = createdMap.ID,
                        Status = Status.Success
                    };
                }
                else
                    return new BaseResponse<Guid>() { Status = Status.Conflict.Clone("A map by that lookup already exists.") };
            });
        }

        public virtual async Task<List<Location>> ListDefaultLocations()
        {
            return await withG(async (client, g) =>
            {
                var query = g.V().HasLabel(AmblOnGraphConstants.AmblOnDefaultsVertexName)
                    .Has(AmblOnGraphConstants.PartitionKeyName, "AmblOn")
                    .Out(AmblOnGraphConstants.ConsumesEdgeName)
                    .HasLabel(AmblOnGraphConstants.LocationVertexName);

                var results = await Submit<Location>(query);

                return results.ToList();
            });
        }

        public virtual async Task<List<Location>> ListMapLocations(string email, Guid mapId)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email);

                var query = g.V(userId)
                    .Out(AmblOnGraphConstants.ConsumesEdgeName)
                    .HasLabel(AmblOnGraphConstants.MapVertexName)
                    .Has("id", mapId)
                    .Out(AmblOnGraphConstants.ConsumesEdgeName)
                    .HasLabel(AmblOnGraphConstants.LocationVertexName);

                var locations = await Submit<Location>(query);

                return locations.ToList();
            });
        }

        public virtual async Task<List<Map>> ListMaps(string email)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email);

                var query = g.V(userId)
                    .Out(AmblOnGraphConstants.ConsumesEdgeName)
                    .HasLabel(AmblOnGraphConstants.MapVertexName);

                var maps = await Submit<Map>(query);

                return maps.ToList();
            });
        }

         public virtual async Task<Status> SetPrimaryMap(string email, Guid mapId)
        {
            return await withG(async (client, g) =>
            {
                var userId = await ensureAmblOnUser(g, email);

                var oldMapsQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.MapVertexName)
                    .Property("Primary", false);

                await Submit(oldMapsQuery);

                var primaryMapQuery = g.V(userId)
                    .Out(AmblOnGraphConstants.OwnsEdgeName)
                    .HasLabel(AmblOnGraphConstants.MapVertexName)
                    .Has("id", mapId)
                    .Property("Primary", true);

                await Submit(primaryMapQuery);

                return Status.Success;
            });
        }

        
        #endregion

        #region Helpers
        public virtual async Task<Guid> ensureAmblOnUser(GraphTraversalSource g, string email)
        {
            var partKey = email?.Split('@')[1];

            var query = g.V().HasLabel(AmblOnGraphConstants.AmblOnUserVertexName)
                .Has(AmblOnGraphConstants.PartitionKeyName, partKey)
                .Has("Email", email);

            var results = await Submit<BusinessModel<Guid>>(query);

            var existingUser = results.Any() ? results.FirstOrDefault().ID : Guid.Empty;

            if (!results.Any())
            {
                query = g.AddV(AmblOnGraphConstants.AmblOnUserVertexName)
                    .Property(AmblOnGraphConstants.PartitionKeyName, partKey)
                    .Property("Email", email);

                results = await Submit<BusinessModel<Guid>>(query);

                existingUser = results.Any() ? results.FirstOrDefault().ID : Guid.Empty;
            }

            return existingUser;
        }
        #endregion
    }

}