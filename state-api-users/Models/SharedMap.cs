
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LCU.Graphs;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class SharedMap : AmblOnVertex
    {   
        [DataMember]
        public virtual Guid DefaultLayerID {get; set;}

        [DataMember]
        public virtual bool Deletable {get; set;}

        [DataMember]
        public virtual string Lookup { get; set; }

        [DataMember]
        public virtual bool Primary {get; set;}

        [DataMember]
        public virtual string Title { get; set; }
    }
}