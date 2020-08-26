
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using AmblOn.State.API.Users.Models;
using Fathym;
using LCU.Graphs.Registry.Enterprises;

namespace AmblOn.State.API.Locations.State
{
    [Serializable]
    [DataContract]
    public class LocationsState
    {
        #region Constants

        #endregion

        [DataMember]
        public virtual List<Location> AllUserLocations { get; set; }

        [DataMember]
        public virtual string Error {get; set;}

        [DataMember]
        public virtual bool Loading { get; set; }

        [DataMember]
        public virtual List<UserLocation> LocalSearchUserLocations {get; set;}

        [DataMember]
        public virtual List<UserLocation> OtherSearchUserLocations {get; set;}

        [DataMember]
        public virtual List<Guid> SelectedUserLayerIDs {get; set;}

        [DataMember]
        public virtual UserInfo UserInfo {get; set;}
        
        [DataMember]
        public virtual List<UserLocation> VisibleUserLocations {get; set;}
    }
}