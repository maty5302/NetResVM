using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ApiEVE
{
    public class ApiEVENode
    {
        public async Task<(HttpStatusCode code,string? nodes)> GetAllNodes(ApiEVEHttpClient client, string labName)
        {
            var url = $"{client.Url}labs/{labName}/nodes";
            var response = await client.Client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return (response.StatusCode,await response.Content.ReadAsStringAsync());
            }
            return (response.StatusCode, null);
        }

        public async Task<HttpStatusCode> StartNode(ApiEVEHttpClient client, string labName, int id)
        {
            var url = $"{client.Url}labs/{labName}/nodes/{id}/start";
            var response = await client.Client.GetAsync(url);
            return response.StatusCode;
        }

        public async Task<HttpStatusCode> StopNode(ApiEVEHttpClient client, string labName, int id)
        {
            var url = $"{client.Url}labs/{labName}/nodes/{id}/stop";
            var response = await client.Client.GetAsync(url);
            return response.StatusCode;
        }
    }
}
