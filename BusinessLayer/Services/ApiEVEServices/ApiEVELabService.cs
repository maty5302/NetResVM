using ApiEVE;
using BusinessLayer.Models;
using Microsoft.AspNetCore.Http;
using SimpleLogger;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace BusinessLayer.Services.ApiEVEServices
{
    /// <summary>
    /// This class is responsible for managing labs in the EVE Online API.
    /// </summary>
    public class ApiEVELabService
    {
        private readonly ApiEVEAuthService apiEVEAuthService;
        private readonly ApiEVELab apiEVELab;
        private static ILogger _logger = FileLogger.Instance; // Logger instance for logging errors and information

        public ApiEVELabService()
        {
            this.apiEVEAuthService = new ApiEVEAuthService();
            this.apiEVELab = new ApiEVELab();
        }


        /// <summary>
        /// Asynchronously retrieves a list of labs associated with a specified server.
        /// </summary>
        /// <param name="serverId">The unique identifier (ID) of the server to retrieve the labs from.</param>
        /// <returns>
        /// A list of <see cref="EVELabModel"/> objects representing the labs on the server.
        /// Returns <c>null</c> if no labs are found or an error occurs.
        /// </returns>
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

        /// <summary>
        /// Asynchronously retrieves detailed information about a specific lab.
        /// </summary>
        /// <param name="client">An instance of <see cref="ApiEVEHttpClient"/> used to communicate with the server.</param>
        /// <param name="serverId">The unique identifier of the EVE-NG server.</param>
        /// <param name="labName">The name of the lab to retrieve information for.</param>
        /// <param name="path">The file path to the lab on the server.</param>
        /// <param name="date">An optional timestamp representing the last modified date to compare or filter by.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="EVELabModel"/> with the lab details, or null if not found.</returns>
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

        /// <summary>
        /// Retrieves detailed information about a specific lab by its ID.
        /// </summary>
        /// <param name="serverId">The unique identifier of the EVE-NG server.</param>
        /// <param name="labId"> The unique identifier of the EVE-NG lab </param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="EVELabModel"/> with the lab details, or null if the lab is not found.</returns>
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

        /// <summary>
        /// Downloads a lab from the EVE-NG server.
        /// </summary>
        /// <param name="lab"> Model of the lab that has all information about it </param>
        /// <param name="serverId">The unique identifier of the EVE-NG server.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a byte array of the lab content if successful, or null if the download fails.</returns>
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

        /// <summary>
        /// Imports a lab into the EVE-NG server.
        /// </summary>
        /// <param name="serverId">The unique identifier of the EVE-NG server.</param>
        /// <param name="file"> File (ZIP) that needs to be imported </param>
        /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> if the lab was successfully imported; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Imports a lab into the EVE-NG server.
        /// </summary>
        /// <param name="serverId">The unique identifier of the EVE-NG server.</param>
        /// <param name="fileContent"> Content of the file </param>
        /// <param name="filename"> Name of the file </param>
        /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> if the lab was successfully imported; otherwise, <c>false</c>.</returns>
        public async Task<bool> ImportLab(int serverId, byte[] fileContent, string filename)
        {
            var client = await apiEVEAuthService.AuthenticateAndCreateClient(serverId);
            if (client.client == null)
            {
                return false;
            }

            try {
                return await apiEVELab.ImportLab(client.client, fileContent, filename);
            }
            catch (Exception e)
            {
                _logger.LogError($"ApiEVELabService - ImportLab - {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes a lab from the EVE-NG server.
        /// </summary>
        /// <param name="serverId">The unique identifier of the EVE-NG server.</param>
        /// <param name="fileName"> Name of the file on server that needs to be deleted</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> if the lab was successfully deleted; otherwise, <c>false</c>.</returns>
        public async Task<bool> DeleteLab(int serverId, string fileName)
        {
            var client = await apiEVEAuthService.AuthenticateAndCreateClient(serverId);
            if (client.client == null)
            {
                return false;
            }
            try {
                return await apiEVELab.DeleteLab(client.client, fileName);
            }
            catch (Exception e)
            {
                _logger.LogError($"ApiEVELabService - DeleteLab - {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks the state of a lab by its name.
        /// </summary>
        /// <param name="serverId">The unique identifier of the EVE-NG server.</param>
        /// <param name="labName"> Name of the lab that we want state of </param>
        /// <returns>A task that represents the asynchronous operation. The task result is an integer representing the state of the specified lab.</returns>
        public async Task<int> StateOfLab(int serverId, string labName)
        {
            ApiEVENodeService service = new ApiEVENodeService();
            var nodes = await service.GetAllNodes(serverId, labName);
            if (nodes == null)
            {
                // No nodes found or error occurred
                return -1; 
            }
            foreach (var node in nodes)
            {
                if (node.Status == 2) // Node is running
                    return 2;
            }
            // All nodes are stopped
            return 0;
        }
    }

}
