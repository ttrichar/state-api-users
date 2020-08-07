
using System;
using System.Runtime.Serialization;
using Fathym.Business.Models;
using System.Collections.Generic;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class SelectedLocation : Location
    {
        
        [DataMember]
        public virtual Guid MapID  { get; set; }
    }
}