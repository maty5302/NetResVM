using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiCisco
{
    public class UserHttpClient
    {
        public string Url { get; private set; }
        public HttpClient Client { get; private set; }

        public UserHttpClient(string url)
        {
            if(url.Last() != '/')
                this.Url = "https://" + url + "/api/v0/";
            else
                this.Url = "https://" + url + "api/v0/";
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            Client = new HttpClient(handler){Timeout = TimeSpan.FromSeconds(20)};
        }
    }
}
