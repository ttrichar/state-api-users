
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LCU.Graphs;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class ActivityGroup : AmblOnVertex
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
        public virtual int Order {get; set;}

        [DataMember]
        public virtual string Title {get; set;}
    }
}