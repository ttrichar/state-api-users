using System;
using System.Runtime.Serialization;
using Fathym.Business.Models;
using System.Collections.Generic;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class VisibleCurations : BusinessModel<Guid>
    {
        
        [DataMember]
        public virtual Guid[] LocationIDs  { get; set; }
    }
}