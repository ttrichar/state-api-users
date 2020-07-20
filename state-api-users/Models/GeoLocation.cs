using System.Runtime.Serialization;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class GeoLocation
    {
        [DataMember]
        public virtual float Latitude { get; set; }
        
        [DataMember]
        public virtual float Longitude { get; set; }
    }
}