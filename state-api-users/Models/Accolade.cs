
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LCU.Graphs;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class Accolade : AmblOnVertex
    {
        [DataMember]
        public virtual Guid LocationID { get; set; }

        [DataMember]
        public virtual string Lookup { get; set; }

        [DataMember]
        public string Rank { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public virtual string Year { get; set; }


    }
}