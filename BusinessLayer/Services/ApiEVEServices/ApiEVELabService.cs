using ApiEVE;
using BusinessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
            if(client.client == null)
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
                List<(string fileName,DateTime modified)> fileNames = new List<(string,DateTime)>();
                string dateFormat = "dd MMM yyyy HH:mm";
                // Loop through the labs array and get the file names
                foreach (var lab in labsElement.EnumerateArray())
                {
                    string? file = lab.GetProperty("file").GetString();
                    string? mtimestring = lab.GetProperty("mtime").GetString();
                    if (file == null || mtimestring == null)
                        continue;
                    DateTime date = DateTime.ParseExact(mtimestring, dateFormat, System.Globalization.CultureInfo.InvariantCulture);
                    fileNames.Add((file,date)); // Add the file name to the list
                }

                // Create a list to store the lab models
                List<EVELabModel> labModels = new List<EVELabModel>();
                foreach (var item in fileNames)
                {
                    var lab = await GetLabInfo(serverId, item.fileName);
                    if (lab != null)
                    {
                        lab.Last_modified = item.modified;
                        labModels.Add(lab);
                    }
                }
                return labModels;               

            }
            return null;

        }    

        public async Task<EVELabModel?> GetLabInfo(int serverId, string labName)
        {
            var client = await apiEVEAuthService.AuthenticateAndCreateClient(serverId);
            if (client.client == null)
            {
                return null;
            }
            var labInfo = await apiEVELab.GetLabInfo(client.client, labName);
            if (labInfo != null)
            {
                var jsonDoc = JsonDocument.Parse(labInfo);
                var labElement = jsonDoc.RootElement.GetProperty("data");
                var lab = JsonSerializer.Deserialize<EVELabModel>(labElement);
                if(lab != null)
                    return lab;
            }
            return null;
        }
    }
}
