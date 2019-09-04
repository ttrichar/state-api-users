
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Fathym.Business.Models;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class Itinerary : BusinessModel<Guid>
    {   
        [DataMember]
        public virtual DateTime EndDate {get; set;}

        [DataMember]
        public virtual string Lookup {get; set;}

        [DataMember]
        public virtual DateTime StartDate {get; set;}

        [DataMember]
        public string Title { get; set; }
    }
}