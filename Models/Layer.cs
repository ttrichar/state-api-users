
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Fathym.Business.Models;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class Layer : BusinessModel<Guid>
    {   
        [DataMember]
        public virtual string Lookup { get; set; }

        [DataMember]
        public virtual string Title { get; set; }
    }
}