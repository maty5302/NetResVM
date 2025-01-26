using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApiEVE
{
    //komunitni verze čili jen http
    public class ApiEVEAuthentication
    {
        public async Task<HttpResponseMessage> Authenticate(ApiEVEHttpClient client,string username, string password)
        {
            var loginUrl = $"{client.Url}auth/login";
            var authData = new
            {
                username = username,
                password = password
            };

            var content = new StringContent(JsonSerializer.Serialize(authData), Encoding.UTF8,"application/json");
            var response = await client.Client.PostAsync(loginUrl, content);
            response.EnsureSuccessStatusCode();
            return response;
        }

        public async Task<HttpResponseMessage> Logout(ApiEVEHttpClient client)
        {
            var logoutUrl = $"{client.Url}auth/logout";
            var response = await client.Client.GetAsync(logoutUrl);
            return response;
        }
    }
}
