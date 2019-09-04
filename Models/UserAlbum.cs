
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Fathym.Business.Models;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class UserAlbum : BusinessModel<Guid>
    {
        [DataMember]
        public virtual List<UserPhoto> Photos {get; set;}

        [DataMember]
        public string Title { get; set; }
    }
}