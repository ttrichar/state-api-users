
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LCU.Graphs;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class Layer : AmblOnVertex
    {   
        [DataMember]
        public virtual string Lookup { get; set; }

        [DataMember]
        public virtual string Title { get; set; }
    }
}