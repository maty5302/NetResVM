using ApiEVE;
using BusinessLayer.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BusinessLayer.Services.ApiEVEServices
{
    public class ApiEVELabService
    {
        private readonly ApiEVEAuthService apiEVEAuthService;
        private readonly ApiEVELab apiEVELab;

        public ApiEVELabService()
        {
            this.apiEVEAuthService = new ApiEVEAuthService();
            this.apiEVELab = new ApiEVELab();
        }

        public async Task<List<EVELabModel>?> GetLabs(int serverId)
        {
            var client = await apiEVEAuthService.AuthenticateAndCreateClient(serverId);
            if (client.client == null)
            {
                return null;
            }
            var labs = await apiEVELab.GetLabs(client.client);
            if (labs != null)
            {
                var jsonDoc = JsonDocument.Parse(labs);

                // Extract the 'labs' part
                var labsElement = jsonDoc.RootElement.GetProperty("data").GetProperty("labs");

                // Create a list to store the file names
                List<(string fileName, string path, DateTime mtimestring)> fileNames = new List<(string, string, DateTime)>();
                string dateFormat = "dd MMM yyyy HH:mm";
                // Loop through the labs array and get the file names
                foreach (var lab in labsElement.EnumerateArray())
                {
                    string? file = lab.GetProperty("file").GetString();
                    string? path = lab.GetProperty("path").GetString();
                    string? mtimestring = lab.GetProperty("mtime").GetString();
                    DateTime date = DateTime.ParseExact(mtimestring, dateFormat, System.Globalization.CultureInfo.InvariantCulture);
                    if (file == null || path == null || mtimestring == null)
                        continue;
                    fileNames.Add((file, path, date)); // Add the file name to the list
                }

                // Create a list to store the lab models
                List<EVELabModel> labModels = new List<EVELabModel>();
                foreach (var item in fileNames)
                {
                    var lab = await GetLabInfo(client.client, serverId, item.fileName, item.path, item.mtimestring);
                    if (lab != null)
                    {
                        labModels.Add(lab);
                    }
                }
                return labModels;

            }
            return null;

        }

        private async Task<EVELabModel?> GetLabInfo(ApiEVEHttpClient client, int serverId, string labName, string path, DateTime? date)
        {
            if (client == null)
            {
                return null;
            }
            var labInfo = await apiEVELab.GetLabInfo(client, labName);
            if (labInfo != null)
            {
                var jsonDoc = JsonDocument.Parse(labInfo);
                var labElement = jsonDoc.RootElement.GetProperty("data");
                var lab = JsonSerializer.Deserialize<EVELabModel>(labElement);
                if (lab != null)
                {

                    if (path != null && date != null)
                    {
                        lab.Path = path;
                        lab.Last_modified = date.Value;
                    }
                    return lab;
                }
            }
            return null;
        }

        public async Task<EVELabModel?> GetLabInfoById(int serverId, string labId)
        {
            var allLabs = await GetLabs(serverId);
            if (allLabs == null)
                return null;
            foreach (var lab in allLabs)
            {
                if (lab.Id == labId)
                    return lab;
            }
            return null;
        }

        public async Task<byte[]?> DownloadLab(EVELabModel lab, int serverId)
        {
            var client = await apiEVEAuthService.AuthenticateAndCreateClient(serverId);
            if (client.client == null || lab.Path == null)
            {
                return null;
            }
            string pathOnly = new string(lab.Path.Except(lab.Filename).ToArray());
            var obj = new Dictionary<string, string>
            {
                { "\"0\"", lab.Path },
                { "path", pathOnly }
            };
            string jsonData = JsonSerializer.Serialize(obj);
            var file = await apiEVELab.ExportLab(client.client, jsonData);
            if (file != null)
            {
                using (var ms = new MemoryStream())
                {
                    using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                    {
                        var entry = archive.CreateEntry(lab.Filename);
                        using (var entryStream = entry.Open())
                        {
                            using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                            {
                                writer.Write(file);
                            }
                        }
                    }
                    return ms.ToArray();
                }
            }
            return null;
        }

        public async Task<bool> ImportLab(int serverId, IFormFile file)
        {
            var client = await apiEVEAuthService.AuthenticateAndCreateClient(serverId);
            if (client.client == null)
            {
                return false;
            }
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                return await apiEVELab.ImportLab(client.client, ms.ToArray(), file.FileName);
            }
        }
    }

}
