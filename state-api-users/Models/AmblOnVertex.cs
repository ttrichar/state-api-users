
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LCU.Graphs;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class AmblOnVertex : LCUVertex
    {
        [DataMember]
        public virtual string PartitionKey { get; set; }
    }
}