using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ApiEVE
{
    public class ApiEVEHttpClient
    {
        public string Url { get; private set; }
        private readonly CookieContainer cookieContainer;
        public HttpClient Client { get; private set; }

        public ApiEVEHttpClient(string url)
        {
            if (url.StartsWith("http://") || url.StartsWith("https://"))
                this.Url = url.TrimEnd('/') + "/api/";
            else
                this.Url = "http://" + url.TrimEnd('/') + "/api/";

            cookieContainer=new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler()
            {
                CookieContainer = cookieContainer,
                UseCookies = true
            };

            this.Client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            Client.DefaultRequestHeaders.Add("Accept", "application/json");
        }
    }
}
