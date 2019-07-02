using AmblOn.State.API.Users.Models;
using Fathym;
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
        #endregion

        #region Helpers
        #endregion
    }

}