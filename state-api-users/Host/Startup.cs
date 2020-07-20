using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LCU.Personas.Client.Applications;
using LCU.Personas.Client.Enterprises;
using LCU.Personas.Client.Identity;
using LCU.Personas.Client.Security;
using System.Linq;
using System;
using LCU.StateAPI;
using AmblOn.State.API.Users.Graphs;
using LCU.Graphs;
using LCU;
using LCU.StateAPI.Hosting;

[assembly: FunctionsStartup(typeof(AmblOn.State.API.Users.Host.Startup))]

namespace AmblOn.State.API.Users.Host
{
    public class Startup : StateAPIStartup
    {
        #region Fields
        #endregion

        #region Constructors
        public Startup()
        { }
        #endregion

        #region API Methods
        public override void Configure(IFunctionsHostBuilder builder)
        {
            base.Configure(builder);

            var loggerFactory = new LoggerFactory();

            var amblGraph = new AmblOnGraph(new GremlinClientPoolManager(
                new ApplicationProfileManager(
                    Environment.GetEnvironmentVariable("LCU-DATABASE-CLIENT-POOL-SIZE").As<int>(4),
                    Environment.GetEnvironmentVariable("LCU-DATABASE-CLIENT-MAX-POOL-CONNS").As<int>(32),
                    Environment.GetEnvironmentVariable("LCU-DATABASE-CLIENT-TTL").As<int>(60)
                ),
                new LCUGraphConfig()
                {
                    APIKey = Environment.GetEnvironmentVariable("LCU-GRAPH-API-KEY"),
                    Database = Environment.GetEnvironmentVariable("LCU-GRAPH-DATABASE"),
                    Graph = Environment.GetEnvironmentVariable("LCU-GRAPH"),
                    Host = Environment.GetEnvironmentVariable("LCU-GRAPH-HOST")
                })
            );

            builder.Services.AddSingleton(amblGraph);

            // appMgr.RegisterApplicationProfile(details.ApplicationID, new LCU.ApplicationProfile()
            // {
            //     DatabaseClientMaxPoolConnections = Environment.GetEnvironmentVariable("LCU-DATABASE-CLIENT-MAX-POOL-CONNS").As<int>(32),
            //     DatabaseClientPoolSize = Environment.GetEnvironmentVariable("LCU-DATABASE-CLIENT-POOL-SIZE").As<int>(4),
            //     DatabaseClientTTLMinutes = Environment.GetEnvironmentVariable("LCU-DATABASE-CLIENT-TTL").As<int>(60)
            // });
        }
        #endregion
    }
}