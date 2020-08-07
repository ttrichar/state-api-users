
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Fathym.Business.Models;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class Album : BusinessModel<Guid>
    {
        [DataMember]
        public virtual string Lookup {get; set;}

        [DataMember]
        public string Title { get; set; }
    }
}