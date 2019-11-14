using System;
using System.Runtime.Serialization;
using Fathym.Business.Models;
using System.Collections.Generic;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class ExcludedCurations : BusinessModel<Guid>
    {
        
        [DataMember]
        public virtual string[] LocationIDs  { get; set; }
    }
}