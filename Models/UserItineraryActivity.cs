
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Fathym.Business.Models;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class UserItineraryActivity : BusinessModel<Guid>
    {
        [DataMember]
        public virtual string ActivityName {get; set;}

        [DataMember]
        public virtual DateTime EndDateTime {get; set;}

        [DataMember]
        public virtual Guid LocationID {get; set;}

        [DataMember]
        public virtual DateTime StartDateTime { get; set; }
    }
}