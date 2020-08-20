
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using AmblOn.State.API.Users.Models;
using Fathym;
using LCU.Graphs.Registry.Enterprises;

namespace AmblOn.State.API.Users.State
{
    [Serializable]
    [DataContract]
    public class UsersState
    {
        #region Constants
        public const string HUB_NAME = "users";
        #endregion
        
        [DataMember]
        public virtual string AddMapError { get; set; }

        [DataMember]
        public virtual List<Location> AllUserLocations { get; set; }

        [DataMember]
        public virtual string Error {get; set;}

        [DataMember]
        public virtual ExcludedCurations ExcludedCuratedLocations { get; set; }

        [DataMember]
        public virtual bool Loading { get; set; }

        [DataMember]
        public virtual List<UserLocation> LocalSearchUserLocations {get; set;}

        [DataMember]
        public virtual List<UserLocation> OtherSearchUserLocations {get; set;}

        [DataMember]
        public virtual List<Guid> SelectedUserLayerIDs {get; set;}

        [DataMember]
        public virtual Guid SelectedUserMapID {get; set;}

        [DataMember]
        public virtual Status SharedStatus {get; set;}

        [DataMember]
        public virtual List<UserAccolade> UserAccolades { get; set; }

        [DataMember]
        public virtual List<UserAlbum> UserAlbums {get; set;}

        [DataMember]
        public virtual UserInfo UserInfo {get; set;}

        [DataMember]
        public virtual List<Itinerary> UserItineraries {get; set;}

        [DataMember]
        public virtual List<UserLayer> UserLayers {get; set;}

        [DataMember]
        public virtual List<UserMap> UserMaps {get; set;}

        [DataMember]
        public virtual List<UserTopList> UserTopLists {get; set; }
        
        [DataMember]
        public virtual List<UserLocation> VisibleUserLocations {get; set;}
    }
}