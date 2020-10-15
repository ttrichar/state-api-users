using System;
using System.Runtime.Serialization;
using LCU.Graphs;
using System.Collections.Generic;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class ExcludedCurations : AmblOnVertex
    {
        
        [DataMember]
        public virtual string LocationIDs  { get; set; }
    }
}