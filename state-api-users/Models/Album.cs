
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LCU.Graphs;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class Album : AmblOnVertex
    {
        [DataMember]
        public virtual string Lookup {get; set;}

        [DataMember]
        public string Title { get; set; }
    }
}