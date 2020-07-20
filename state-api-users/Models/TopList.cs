using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Fathym.Business.Models;
using LCU.Presentation;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class TopList : BusinessModel<Guid>
    {
        [DataMember]
        public virtual List<Location> LocationList { get; set; }

        [DataMember]
        public string OrderedValue { get; set; }

        [DataMember]
        public string Title { get; set; }
        
    }
}