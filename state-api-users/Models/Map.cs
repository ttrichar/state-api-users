
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LCU.Graphs;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class Map : AmblOnVertex
    {   
        [DataMember]
        public virtual string Coordinates {get; set;}

        [DataMember]
        public virtual Guid DefaultLayerID {get; set;}

        [DataMember]
        public virtual float Latitude { get; set; }
        
        [DataMember]
        public virtual float Longitude { get; set; }

        [DataMember]
        public virtual string Lookup { get; set; }

        [DataMember]
        public virtual bool Primary {get; set;}
        
        [DataMember]
        public virtual string Title { get; set; }
        
        [DataMember]
        public virtual int Zoom { get; set; }
    }
}