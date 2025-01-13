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
        public static async Task<string[]?> GetLabs(UserHttpClient user)
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

        public static async Task<LabModel?> GetLabInfo(UserHttpClient user, string labId)
        {
            var url = $"{user.Url}labs/{labId.Trim()}";
            try
            {
                var response = await user.Client.GetAsync(url.Trim());
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync().Result;
                    var lab = JsonSerializer.Deserialize<LabModel>(data);
                    return lab;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Console.WriteLine("Lab not found");
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

        public static async Task<string?> DownloadLab(UserHttpClient user, string labId)
        {
            var url = user.Url + "labs/" + labId.Trim() + "/download";
            try
            {
                var response = await user.Client.GetAsync(url.Trim());
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync().Result;
                    return data;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Console.WriteLine("Lab not found");
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

        public static async Task<(bool,string)> StartLab(UserHttpClient user, string labId)
        {
            var labs = GetLabs(user).Result;
            if (labs == null || !labs.Contains(labId))
                return (false,"Lab not found");
            foreach (var item in labs)
            {
                var labInfo = GetLabInfo(user, item);
                if (labInfo.Result != null)
                {
                    if (labInfo.Result.Id == labId && labInfo.Result.State.ToLower() == "started")
                        return (true,"");
                    else if (labInfo.Result.State.ToLower() == "started")
                        return (false, "Another lab is already running..");
                }
            }
            var url = user.Url + "labs/" + labId.Trim() + "/start";
            try
            {
                var response = await user.Client.PutAsync(url.Trim(), null);
                if (response.StatusCode == HttpStatusCode.NoContent)
                    return (true, "Lab started successfully");
                else
                    return (false, response.StatusCode.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to start the lab with ID {labId}.", ex);
            }
        }

        public static async Task<bool> StopLab(UserHttpClient user, string labId)
        {
            var url = user.Url + "labs/" + labId.Trim() + "/stop";
            try
            {
                var response = await user.Client.PutAsync(url.Trim(), null);
                if (response.StatusCode == HttpStatusCode.NoContent)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to stop the lab with ID {labId}.", ex);
            }
        }

        public static async Task<bool> StopAllLabs(UserHttpClient user)
        {
            var labs = GetLabs(user).Result;          
            try
            {
                if (labs != null)
                {
                    foreach (var item in labs)
                    {
                        var url = user.Url + "labs/" + item.Trim() + "/stop";
                        var response = await user.Client.PutAsync(url.Trim(), null);
                        if (response.StatusCode == HttpStatusCode.NoContent)
                            continue;
                        else
                            return false;
                    }
                    return true;
                }
                return false;
            }   
            catch (Exception ex)
            {
                throw new Exception("Failed to stop all labs.", ex);
            }
        }

        public static async Task<bool> ImportLab(UserHttpClient user, string file)
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
    }
}
