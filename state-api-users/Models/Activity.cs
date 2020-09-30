
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LCU.Graphs;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class Activity : AmblOnVertex
    {   
        [DataMember]
        public virtual bool Checked { get; set; }

        [DataMember]
        public virtual DateTime CreatedDateTime {get; set;}

        [DataMember]
        public virtual bool Editable { get; set; }

        [DataMember]
        public virtual bool Favorited { get; set; }

        [DataMember]
        public virtual Guid? LocationID {get; set;}

        [DataMember]
        public virtual string Notes { get; set; }

        [DataMember]
        public virtual int Order {get; set;}

        [DataMember]
        public virtual string Title {get; set;}

        [DataMember]
        public virtual string Lookup {get; set;}

        [DataMember]
        public virtual string TransportIcon { get; set; }

        [DataMember]
        public virtual string WidgetIcon { get; set; }
    }
}