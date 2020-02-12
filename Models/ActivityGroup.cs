
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Fathym.Business.Models;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class ActivityGroup : BusinessModel<Guid>
    {   
        [DataMember]
        public virtual List<Activity> Activities {get; set;}

        [DataMember]
        public virtual bool Checked {get; set;}

        [DataMember]
        public virtual DateTime CreatedDateTime {get; set;}

        [DataMember]
        public virtual string GroupType {get; set;}

        [DataMember]
        public virtual bool Editable {get; set;}

        [DataMember]
        public virtual string Title {get; set;}
    }
}