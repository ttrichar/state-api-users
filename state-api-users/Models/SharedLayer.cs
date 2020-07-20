
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Fathym.Business.Models;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class SharedLayer : BusinessModel<Guid>
    {   
        [DataMember]
        public virtual Guid DefaultMapID {get; set;}

        [DataMember]
        public virtual bool Deletable {get; set;}

        [DataMember]
        public virtual string Lookup { get; set; }

        [DataMember]
        public virtual string Title { get; set; }
    }
}