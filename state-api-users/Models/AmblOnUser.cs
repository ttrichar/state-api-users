
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LCU.Graphs;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class AmblOnUser : AmblOnVertex
    {   
        [DataMember]
        public virtual string Email { get; set; }
    }
}