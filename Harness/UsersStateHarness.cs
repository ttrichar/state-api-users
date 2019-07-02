using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AmblOn.State.API.Users.Graphs;
using AmblOn.State.API.Users.Models;
using Fathym;
using Fathym.Design.Singleton;
using LCU.Graphs.Registry.Enterprises;
using LCU.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace AmblOn.State.API.Users.Harness
{
    public class UsersStateHarness : LCUStateHarness<UsersState>
    {
        #region Fields
        protected readonly AmblOnGraph amblGraph;
        #endregion

        #region Properties
        #endregion

        #region Constructors
        public UsersStateHarness(HttpRequest req, ILogger log, UsersState state)
            : base(req, log, state)
        {
            amblGraph = req.LoadGraph<AmblOnGraph>();
        }
        #endregion

        #region API Methods
        public virtual async Task<UsersState> Ensure()
        {
            state.DefaultLocations = await amblGraph.ListDefaultLocations();
            
            return state;
        }
        #endregion

        #region Helpers
        #endregion
    }
}