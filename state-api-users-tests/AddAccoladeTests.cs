using Fathym.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace state_api_users_tests
{
    [TestClass]
    public class AddAccoladeTests : AzFunctionTestBase
    {
        
        public AddAccoladeTests()
        {
            APIRoute = "api/AddAccolade";    
        }

        [TestMethod, TestProperty("Snapshot", Snapshots.None)]// <--- use the correct snapshot        
        public async Task TestAddAccolade()
        {
            //var url = $"{HostURL}/{APIRoute}/{data.EnterpriseAPIKey}/list";

            var url = "http://www.fathym.com";

            var response = await httpGet(url); 

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var model = getContent<dynamic>(response);

            Assert.IsTrue(true);// <--- assert model content matches the snapshot    

            Console.WriteLine($"Successfully pinged {url}");                    
        }
    }
}
