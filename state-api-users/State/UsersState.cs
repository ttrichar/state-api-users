
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using AmblOn.State.API.Users.Models;
using Fathym;
using LCU.Graphs.Registry.Enterprises;

namespace AmblOn.State.API.Users.State
{
    [Serializable]
    [DataContract]
    public class UsersState
    {
        #region Constants
        public const string HUB_NAME = "users";
        #endregion
    }
}