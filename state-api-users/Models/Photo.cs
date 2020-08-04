
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Fathym.Business.Models;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class Photo : BusinessModel<Guid>
    {
        [DataMember]
        public virtual string Caption {get; set;}

        [DataMember]
        public virtual string Lookup {get; set;}

        [DataMember]
        public virtual Guid LocationID {get; set;}

        [DataMember]
        public virtual string URL { get; set; }
    }
}