
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
        public virtual string AddMapError { get; set; }

        [DataMember]
        public virtual bool Loading { get; set; }

        [DataMember]
        public virtual Guid PrimaryMapID {get; set;}

        [DataMember]
        public virtual List<Guid> SelectedMapIDs { get; set; }

        [DataMember]
        public virtual List<SelectedLocation> SelectedMapLocations { get; set; }

        [DataMember]
        public virtual List<Map> UserMaps { get; set; }
    }
}