
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LCU.Graphs;
using LCU.Presentation;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class UserPhoto : AmblOnVertex
    {
        [DataMember]
        public virtual string Caption {get; set;}

        [DataMember]
        public virtual ImageMessage ImageData {get; set;}

        [DataMember]
        public virtual Guid? LocationID {get; set;}

        [DataMember]
        public virtual string URL { get; set; }
    }
}