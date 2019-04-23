
using System.Collections.Generic;
using System.Runtime.Serialization;
using LCU.Graphs.Registry.Enterprises;

namespace AmblOn.State.API.Users.Models
{
    [DataContract]
    public class UsersState
    {
        [DataMember]
        public virtual bool Loading { get; set; }
    }
}