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
    public static class Lab
    {
        public static async Task<string[]?> GetLabs(UserHttpClient user,string token)
        {
            var url = user.Url + "labs";
            try
            { 
                    var response = user.Client.GetAsync(url).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var data = response.Content.ReadAsStringAsync().Result.Split(",");
                        string[] charactersToRemove = { "\n", "\"", "[", "]" };
                        for (int i = 0; i < data.Length; i++)
                        {
                            foreach (string item in charactersToRemove)
                            {
                                data[i] = data[i].Replace(item, string.Empty);
                            }
                        }
                        return await Task.FromResult<string[]?>(data);
                    }
                    return await Task.FromResult<string[]?>(null);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return await Task.FromResult<string[]?>(null);
            }
        }

        public static async Task<LabModel?> GetLabInfo(UserHttpClient user, string labId)
        {
            var url = user.Url + "labs/" + labId.Trim();
            try
            {
                var response = user.Client.GetAsync(url.Trim()).Result;
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync().Result;
                    var lab = JsonSerializer.Deserialize<LabModel>(data);
                    return await Task.FromResult<LabModel?>(lab);
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Console.WriteLine("Lab not found");
                    return await Task.FromResult<LabModel?>(null);
                }
                return await Task.FromResult<LabModel?>(null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return await Task.FromResult<LabModel?>(null);
            }
        }
        
        
    }
}
