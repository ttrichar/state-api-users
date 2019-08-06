
using System;
using System.Runtime.Serialization;
using Fathym.Business.Models;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class Map : BusinessModel<Guid>
    {

        [DataMember]
        public string Lookup { get; set; }
        
        [DataMember]
        public virtual float Latitude { get; set; }
        
        [DataMember]
        public virtual float Longitude { get; set; }

        [DataMember]
        public virtual bool Primary { get; set; }
        
        [DataMember]
        public virtual string Title { get; set; }
        
        [DataMember]
        public int Zoom { get; internal set; }
    }
}