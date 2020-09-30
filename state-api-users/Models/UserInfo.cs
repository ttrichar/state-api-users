using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LCU.Graphs;
using LCU.Presentation;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class UserInfo : AmblOnVertex
    {

        [DataMember]
        public virtual string Country { get; set; }

        [DataMember]
        public virtual string Email {get; set;}
        
        [DataMember]
        public string FirstName { get; set; }

        [DataMember]
        public string LastName { get; set; }

        [DataMember]
        public virtual string Lookup { get; set; }

        [DataMember]
        public string Zip { get; set; }
        
    }
}