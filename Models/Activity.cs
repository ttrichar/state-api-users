
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Fathym.Business.Models;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class Activity : BusinessModel<Guid?>
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
        public virtual string Title {get; set;}

        [DataMember]
        public virtual string TransportIcon { get; set; }

        [DataMember]
        public virtual string WidgetIcon { get; set; }
    }
}