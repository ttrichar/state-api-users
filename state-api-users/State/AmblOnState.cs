
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using AmblOn.State.API.Users.Models;
using Fathym;
using LCU.Graphs.Registry.Enterprises;

namespace AmblOn.State.API.AmblOn.State
{
    [Serializable]
    [DataContract]
    public class AmblOnState
    {
        #region Constants
        public const string HUB_NAME = "amblon";
        #endregion
    }
}