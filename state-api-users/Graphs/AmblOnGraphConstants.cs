using Fathym;
using LCU.Graphs;
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
    public class AmblOnGraphConstants
    {
        public const string AccoladeVertexName = "Accolade";

		public const string ActivityGroupVertexName = "ActivityGroup";

		public const string ActivityVertexName = "Activity";

        public const string AlbumVertexName = "Album";

		public const string AmblOnUserVertexName = "AmblOnUser";

		public const string CanViewEdgeName = "CanView";
        
		public const string ContainsEdgeName = "Contains";

		public const string DefaultUserEmail = "default@amblon.com";

		public const string DefaultUserID = "98005bd5-830f-4ab0-8ce6-02906455db01";

		public const string IDPropertyName = "id";

		public const string InheritsEdgeName = "Inherits";

		public const string ItineraryVertexName = "Itinerary";
        
		public const string LayerVertexName = "Layer";

		public const string LocationVertexName = "Location";

		public const string LookupPropertyName = "Lookup";
        
		public const string MapVertexName = "Map";

		public const string OccursAtEdgeName = "OccursAt";
        
		public const string OwnsEdgeName = "Owns";
   
		public const string PartitionKeyName = "PartitionKey";

		public const string PhotoVertexName = "Photo";

		public const string SharedLayerVertexName = "SharedLayer";

		public const string SharedMapVertexName = "SharedMap";

		public const string TopListVertexName = "TopList";

		public const string UserInfoVertexName = "UserInfo";

		public const string ExcludedCurationsName = "ExcludedCurations";
    }

}