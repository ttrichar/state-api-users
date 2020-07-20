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
    public abstract class AzFunctionTestBase : APITestBase
    {                
         public string ClientPrincipalId { get; set; }
         public string ContentType { get; set; }
         public string Cookie { get; set; }
         public string EntApiKey { get; set; }
         public string Environment { get; set; }      
         public string Host { get; set; }
         public string HubName { get; set; }
         public string LcuHost { get; set; }
         public string StateKey { get; set; }
         public string UsernameMock { get; set; }

        
        public AzFunctionTestBase()
        {
            ClientPrincipalId = "";
            ContentType = "application/json";
            Cookie = "___stripe_mid=46348386-7e07-4ee2-9f23-116fe63fad98; .AspNetCore.Cookies=CfDJ8AccS4yuy_5PqBVcean6AlS1nRAXIV46ap6a8vtSJDqLmAGI30JRrDm_IIUCA2KcHYg7dy8v-A6PHrAUIj69IqIwOOKmSpZdWDD_ZcOVaYJQy-HPo8Ilaa1vrSvtU7RAbKn0FDgBvZaa7ZJrw3Nh7uIdDScP2wxDD6i8-xRQG7iOn0nqrxZtFHAa0GyyPC4sJ6OPBG3B40OzvbPY9AJ5hnOESoYi7HGpulSaHmYVAbRZW2ztI9o-E6anx9OvVV1aFFASNSQnBOxdiE_4R3f4dTl81atffC3my7j-12QDYA4-rhw4fM9DjrYELVZvuLIkhmfeHGahdo0TT7Db9SvLj0SvUG0zQItDr19xJwi0xYLSP5-Eun5oYtvj-q15IaadLygF0Wck7U8I3inUbhZyZp8FQt6ZF_-UrUpTnGqLFwmBvvbwmrtUqAAg0WB5wIu8QZycIVejd5v31CqUYjZ05bqUBhr4klP1nJ2cMN78WbYbKZ6QqqZS4vmtH-4mKfZnEWl2pMp0dwOuWrVJjy7_eZjm4EkZ90KlUDSza0TpgJVGc7vAOT7gN6gC-KRJIb8EWbrz-JAc1WrnSXRFHk_7f2HDIdZIZ7nL_qBAqsZDQOqT_tdwJ0VUEbaXTEjfyn9Z79jqU4kfI72VCXAGZD2yLXaLepQ9V_yBnb-KCtouMWYVv-dlhrH8SUIeJsWuORi1XLDNCiPmqWeJ1zLrHAblOeI4Rb_yaKHu1rH8Br5b3IGY2V5sC4IaM36lSIgUekaQhTris4iqlDyMyShRoS-Cv0a65e122sbWmzf3YhJDnaIc5G2_SHkpRSf9BUsGWsNeoujkMd3mTLfoHAKwnOKgu0112EpMeeCkk8axlb14jBFsGxf9ZpudkVUd7oMsutsG9H9v-jFmHuOcTDuZqsmoH1uDxVvqtIMGtQzi2Gp5wMLO_-KEN49DJaWq9jyaa5b29Wo7YoL2wPWHkVw7mqwYLCL9Opnt_Sau1AXDyNC38DbbhhqXlfdbk-XYeZ1kddBgY_8F-w49WLDbPYhWJ39USFBGNmtTK5hizggMjdQKVY-hmV1_kJeEBAAyY_Ivkhhbxkmk3CYdV_rkHutMMrUejjBQ9b40p_Yjq1fyk5ng0RvRQciUSq0OAz0D_yupjWSKNsCwlRQSL7gSo6jKegIxgQB8jjUdwYlJJ_W6Xejc5c61GYQvdSPxE2uirtH9AUrj4fa1K_6h5242_pxNm4KRHM6MN9vQifLprXZhijg_-28ft7RwpXOhDz_AYdu1qtOE9ZPP16IJQHiFKELlR9wFpfaSpLf7DpI9B36hGqEBrsFhczlBivYOTPMpZhzSFFuv71tMsj0-4tUBhegB5oEc6whefJ-4tztBWJPm3ZSR765YJ0ENz5tlF6x3vsLxKdyABHCK-3zFLktUBatYZ4E82LX9tmbKLYON-_YIt-_rAuRuxTauQJv91ph-NGLFt-OXIpBO06uDIiqECtJ0egOhjyIkiJo4vFVon3wISr-knF8nhPtVwQXfsxeoHSydTwb8NQT0MNd5rkmxMOzJ0BgjTQNjkA5hoAh4Ag3hiV7jivS1cRCHLOb5QpjKtoT6vMUDLFxmGxHk91wzsq9M1qoqdR1L3sOgNHATSK8BYzOVTnPmrZ9pWjrKHqRNzo8P5RZ6TtXuUxl6Tnxd0eoiVJvnAppkGD-7IjrV5iJkLh8x_efnGdNM8SSUsm1SreQUkruRQMf3wGvhumSxe8CawqCUbz5wznozgA1YlXPmNBfpwo8c; .AspNetCore.Session=CfDJ8AccS4yuy%2F5PqBVcean6AlSEG8Fc%2FjD%2Fho8TDquSs1pmoRFMoeG2ZNJY%2BhA2V4MvK9VrbtG03cSXfsyHaNx5xkbNCN775TTx%2BXhn1NOPO%2FoOXNiFX8iMYVYcBBAANMCOkwt7veQ3iYV1%2BJ8TCwB%2Bd%2Bb81vfptuZLpq3LPInryRSf; ARRAffinity=fe780a149832f24562d8e89f0bc3508071d450bce6ab83b7e7e04ddee934cb17";
            EntApiKey = "";
            Environment = "";        
            Host = "www.fathym-it.com";
            HubName = "dataflowmanagement";
            LcuHost = "www.fathym-it.com";
            StateKey = "main";
            UsernameMock = "";       
        }

        protected virtual void addDefaultHeaders()
        {
            addHeader("content-type", ContentType);
            addHeader("lcu-ent-api-key", EntApiKey);
            addHeader("lcu-state-key", StateKey);
            addHeader("lcu-hub-name", HubName);
            addHeader("lcu-username-mock", UsernameMock);
            addHeader("lcu-environment", Environment);
            addHeader("cookie", Cookie);
            addHeader("x-ms-client-principal-id", ClientPrincipalId);
            addHeader("host", Host);
            addHeader("lcu-host", LcuHost);
        }
    }
}
