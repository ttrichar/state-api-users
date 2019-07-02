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
    public class AmblOnGraphConstants
    {
		public const string AmblOnDefaultsVertexName = "AmblOnDefaults";
        
		public const string ConsumesEdgeName = "Consumes";
        
		public const string LocationVertexName = "Location";
        
		public const string ManagesEdgeName = "Manages";
        
		public const string OwnsEdgeName = "Owns";
   
		public const string PartitionKeyName = "PartitionKey";
    }

}