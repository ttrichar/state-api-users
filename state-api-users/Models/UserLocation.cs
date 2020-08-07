
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Fathym.Business.Models;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class UserLocation : BusinessModel<Guid?>
    {
        // [DataMember]
        // public virtual List<UserAccolade> Accolades { get; set; }

        [DataMember]
        public virtual string Address { get; set; }

        [DataMember]
        public virtual string Country { get; set; }

        [DataMember]
        public virtual Boolean Deletable { get; set; }

        [DataMember]
        public virtual string GoogleLocationName { get; set; }
        
        [DataMember]
        public virtual string Icon { get; set; }

        [DataMember]
        public virtual string Instagram { get; set; }

        [DataMember]
        public virtual bool IsHidden { get; set; }

        [DataMember]
        public virtual float Latitude { get; set; }

        [DataMember]
        public virtual Guid LayerID { get; set; }
        
        [DataMember]
        public virtual float Longitude { get; set; }

        [DataMember]
        public virtual string Lookup { get; set; }

        [DataMember]
        public virtual string State { get; set; }

        [DataMember]
        public virtual string Telephone { get; set; }
        
        [DataMember]
        public virtual string Title { get; set; }

        [DataMember]
        public virtual string Town { get; set; }

        [DataMember]
        public virtual string Website { get; set; }

        [DataMember]
        public virtual string ZipCode { get; set; }

    }
}