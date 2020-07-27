
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Fathym.Business.Models;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class ActivityLocationLookup : BusinessModel<Guid?>
    {   
        [DataMember]
        public virtual Activity Activity {get; set;}

        [DataMember]
        public virtual UserLocation Location {get; set;}
    }
}