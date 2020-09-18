using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LCU.Graphs;
using LCU.Presentation;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class TopList : AmblOnVertex
    {
        [DataMember]
        public virtual List<Activity> Activities { get; set; }

        [DataMember]
        public virtual List<Location> LocationList { get; set; }

        [DataMember]
        public string OrderedValue { get; set; }

        [DataMember]
        public string Title { get; set; }
        
    }
}