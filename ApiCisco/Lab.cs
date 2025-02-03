using ApiCisco.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApiCisco
{
    public class ApiCiscoLab
    {
        public async Task<string[]?> GetLabs(UserHttpClient user)
        {
            var url = user.Url + "labs";
            try
            {
                var response = await user.Client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync().Result.Split(",");
                    string[] charactersToRemove = { "\n", "\"", "[", "]", " " };
                    for (int i = 0; i < data.Length; i++)
                    {
                        foreach (string item in charactersToRemove)
                        {
                            data[i] = data[i].Replace(item, string.Empty);
                        }
                    }
                    return data;
                }
                return null;

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public async Task<string?> GetLabInfo2(UserHttpClient client, string labId, bool download = false)
        {

            var url = $"{client.Url}labs/{labId.Trim()}";
            if (download)
                url = url + "/download";
            var response = await client.Client.GetAsync(url.Trim());
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
                return null;
        }

        public async Task<(bool result, HttpStatusCode message)> StartStopLab(UserHttpClient client, string labId, bool start = true)
        {
            var url = $"{client.Url}labs/{labId.Trim()}";
            if (start)
                url += "/start";
            else
                url += "/stop";

            var response = await client.Client.PutAsync(url.Trim(), null);
            if (response.StatusCode == HttpStatusCode.NoContent)
                return (true, response.StatusCode);
            else
                return (false, response.StatusCode);
        }

        public async Task<bool> ImportLab(UserHttpClient user, string file)
        {
            var url = user.Url + "import";
            try
            {
                var content = new StringContent(file, Encoding.UTF8, "application/json");
                var response = await user.Client.PostAsync(url.Trim(), content);
                if (response.StatusCode == HttpStatusCode.OK)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to import the lab.", ex);
            }
        }

        public async Task<string?> StateOfLab(UserHttpClient user, string labId)
        {
            var url = $"{user.Url}labs/{labId.Trim()}/state";
            var response = await user.Client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }
        
        public async Task<HttpResponseMessage> DeleteLab(UserHttpClient user, string labId)
        {
            var url = $"{user.Url}labs/{labId.Trim()}";
            var response = await user.Client.DeleteAsync(url);
            return response;
        }
    }
}
