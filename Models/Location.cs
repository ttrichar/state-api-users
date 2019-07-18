
using System;
using System.Runtime.Serialization;
using Fathym.Business.Models;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class Location : BusinessModel<Guid>
    {
        [DataMember]
        public virtual string Address { get; set; }
        
        [DataMember]
        public virtual string Icon { get; set; }
        
        [DataMember]
        public virtual float Latitude { get; set; }
        
        [DataMember]
        public virtual float Longitude { get; set; }
        
        [DataMember]
        public virtual string Title { get; set; }
    }
}