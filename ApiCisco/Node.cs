using ApiCisco.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApiCisco
{
    public static class Node
    {
        public static async Task<string[]?> GetNodes(UserHttpClient user, string labId)
        {
            var url = user.Url + $"labs/{labId}/nodes";
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
        public static async Task<NodeModel?> GetNodeInfo(UserHttpClient user,string labId, string nodeId)
        {
            var url = user.Url + $"labs/{labId}/nodes/{nodeId}";
            try
            {
                var response = await user.Client.GetAsync(url.Trim());
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync().Result;
                    var node = JsonSerializer.Deserialize<NodeModel>(data);
                    return node;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Console.WriteLine("Node not found");
                    return null;
                }
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
    }
}
