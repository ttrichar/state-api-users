
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Fathym.Business.Models;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class AmblOnUser : BusinessModel<Guid>
    {   
        [DataMember]
        public virtual string Email { get; set; }
    }
}