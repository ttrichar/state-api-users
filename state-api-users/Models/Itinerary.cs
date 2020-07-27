
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Fathym.Business.Models;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class Itinerary : BusinessModel<Guid?>
    {   
        
        [DataMember]
        public virtual List<ActivityGroup> ActivityGroups {get; set;}

        [DataMember]
        public virtual bool Editable {get; set;}

        [DataMember]
        public virtual DateTime CreatedDateTime {get; set;}

        [DataMember]
        public virtual bool Shared {get; set;}

        [DataMember]
        public virtual Guid SharedByUserID {get; set;}

        [DataMember]
        public virtual string SharedByUsername {get; set;}

        [DataMember]
        public virtual string Title {get; set;}
    }
}