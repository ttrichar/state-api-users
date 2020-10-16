
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LCU.Graphs;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class ActivityLocationLookup : AmblOnVertex
    {   
        [DataMember]
        public virtual Activity Activity {get; set;}

        [DataMember]
        public virtual Location Location {get; set;}
    }
}