
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LCU.Graphs;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class UserAlbum : AmblOnVertex
    {
        [DataMember]
        public virtual List<UserPhoto> Photos {get; set;}

        [DataMember]
        public string Title { get; set; }
    }
}