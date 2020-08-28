
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using AmblOn.State.API.Users.Models;
using Fathym;
using LCU.Graphs.Registry.Enterprises;

namespace AmblOn.State.API.Itineraries.State
{
    [Serializable]
    [DataContract]
    public class ItinerariesState
    {
        #region Constants

        #endregion

        [DataMember]
        public virtual string Error {get; set;}

        [DataMember]
        public virtual bool Loading { get; set; }

        [DataMember]
        public virtual Status SharedStatus {get; set;}

        [DataMember]
        public virtual string StateType { get; set; } = "Itineraries";

        [DataMember]
        public virtual UserInfo UserInfo {get; set;}

        [DataMember]
        public virtual List<Itinerary> UserItineraries {get; set;}
    }
}