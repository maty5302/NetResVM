using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiEVE
{
    public class ApiEVELab
    {
        public async Task<string?> GetLabs(ApiEVEHttpClient client)
        {
            var url = $"{client.Url}folders/";
            var response = await client.Client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }

        public async Task<string?> GetLabInfo(ApiEVEHttpClient client, string labName)
        {
            var url = $"{client.Url}labs/{labName.Trim()}";
            var response = await client.Client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
                return null;
        }
    }

}
