
using System;
using System.Runtime.Serialization;
using LCU.Graphs;
using System.Collections.Generic;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class SelectedLocation : Location
    {
        
        [DataMember]
        public virtual Guid MapID  { get; set; }
    }
}