
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Fathym.Business.Models;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class UserItinerary : BusinessModel<Guid?>
    {
        [DataMember]
        public virtual List<UserItineraryActivity> Activities {get; set;}

        [DataMember]
        public virtual DateTime EndDate {get; set;}

        [DataMember]
        public virtual DateTime StartDate {get; set;}

        [DataMember]
        public string Title { get; set; }
    }
}