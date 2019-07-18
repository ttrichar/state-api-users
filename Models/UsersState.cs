
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LCU.Graphs.Registry.Enterprises;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class UsersState
    {
        [DataMember]
        public virtual bool Loading { get; set; }

        [DataMember]
        public virtual Guid SelectedMapID { get; set; }

        [DataMember]
        public virtual List<Location> SelectedMapLocations { get; set; }

        [DataMember]
        public virtual List<Map> UserMaps { get; set; }
    }
}