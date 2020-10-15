
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LCU.Graphs;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class UserLayer : AmblOnVertex
    {
        [DataMember]
        public virtual float[] Coordinates {get; set;}

        [DataMember]
        public virtual bool Deletable {get; set;}

        [DataMember]
        public virtual Guid InheritedID {get; set;}

        [DataMember]
        public virtual bool Shared {get; set;}

        [DataMember]
        public string Title { get; set; }
    }
}