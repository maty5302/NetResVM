using ApiCisco;
using SimpleLogger;
using BusinessLayer.Models;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace BusinessLayer.Services.ApiCiscoServices
{
    /// <summary>
    /// Service responsible for managing Cisco CML labs, including operations like retrieving, starting, stopping,
    /// importing, exporting, and deleting labs.
    /// </summary>
    /// <remarks>
    /// Communicates with the Cisco CML API through <see cref="ApiCiscoLab"/> and uses <see cref="ApiCiscoAuthService"/> for authentication.
    /// </remarks>
    public class ApiCiscoLabService
    {
        private readonly ApiCiscoAuthService _authService;
        private readonly ILogger logger = FileLogger.Instance;
        private readonly ApiCiscoLab _labApi;


        public ApiCiscoLabService()
        {
            _authService = new ApiCiscoAuthService();
            _labApi = new ApiCiscoLab();
        }
        /// <summary>
        /// Retrieves a list of all available labs on the specified Cisco CML server.
        /// </summary>
        /// <param name="serverId">The ID of the server from which to retrieve labs.</param>
        /// <returns>
        /// A tuple containing a list of <see cref="CiscoLabModel"/> and a message string.
        /// </returns>
        public async Task<(List<CiscoLabModel>? labs, string Message)> GetLabs(int serverId)
        {
            var client = await _authService.AuthenticateAndCreateClient(serverId);
            if (client.conn == null)
            {
                return (null, client.message);
            }
            var labsID = await _labApi.GetLabs(client.conn);
            List<CiscoLabModel> labs = new List<CiscoLabModel>();
            if (labsID != null)
            {
                foreach (var lab in labsID)
                {
                    var labInfo = await GetLabInfo(serverId, lab, client.conn);
                    if (labInfo.lab != null)
                        labs.Add(labInfo.lab);
                }
                return (labs, "");
            }
            else
            {
                logger.LogError("ApiCiscoLabService - Couldn't fetch labs from cisco API.");
                return (null, "Couldn't fetch labs from cisco API.");
            }

        }

        /// <summary>
        /// Retrieves detailed information about a specific lab from the Cisco CML server.
        /// </summary>
        /// <param name="serverId">The ID of the server hosting the lab.</param>
        /// <param name="labId">The unique identifier of the lab.</param>
        /// <param name="httpClient">Optional existing HTTP client to use for the request.</param>
        /// <returns>
        /// A tuple containing a <see cref="CiscoLabModel"/> and a message string.
        /// </returns>
        public async Task<(CiscoLabModel? lab, string Message)> GetLabInfo(int serverId, string labId, ApiCiscoHttpClient? httpClient = null)
        {
            try
            {
                if (httpClient == null)
                {
                    var client = await _authService.AuthenticateAndCreateClient(serverId);
                    if (client.conn == null)
                    {
                        return (null, "Authentication failed");
                    }
                    httpClient = client.conn;
                }
                var json = await _labApi.GetLabInfo2(httpClient, labId);
                if (json != null)
                {
                    var lab = JsonSerializer.Deserialize<CiscoLabModel>(json);
                    return (lab, "");
                }
                else
                {
                    logger.LogWarning($"ApiCiscoLabService - Lab not found. Lab ID:{labId}");
                    return (null, "Lab not found..");
                }
            }
            catch (Exception e)
            {
                logger.LogError("ApiCiscoLabService - GetLabInfo - " + e.Message);
                return (null, "Something went wrong.. Contact admin.");

            }
        }

        /// <summary>
        /// Downloads the contents of a specified lab as a byte array.
        /// </summary>
        /// <param name="serverId">The ID of the server hosting the lab.</param>
        /// <param name="labId">The unique identifier of the lab.</param>
        /// <returns>A tuple containing the file contents and a message string.</returns>
        public async Task<(byte[]? fileContent, string message)> DownloadLab(int serverId, string labId)
        {
            try
            {
                var client = await _authService.AuthenticateAndCreateClient(serverId);
                if (client.conn == null)
                {
                    return (null, "Authentication failed");
                }
                var data = await _labApi.GetLabInfo2(client.conn, labId, true);
                if (data != null)
                {
                    return (Encoding.UTF8.GetBytes(data), "");
                }
                else
                {
                    logger.LogWarning($"ApiCiscoLabService - Lab not found. Lab ID:{labId}");
                    return (null, "Lab not found..");
                }
            }
            catch (Exception e)
            {
                logger.LogError("ApiCiscoLabService - DownloadLab - " + e.Message);
                return (null, "Something went wrong.. Contact admin.");
            }
        }

        /// <summary>
        /// Starts the specified lab if no other lab is currently running.
        /// </summary>
        /// <param name="serverId">The ID of the server hosting the lab.</param>
        /// <param name="labId">The unique identifier of the lab to start.</param>
        /// <returns>A tuple indicating whether the operation was successful and a message string.</returns>
        public async Task<(bool value, string message)> StartLab(int serverId, string labId)
        {
            try
            {
                var client = await _authService.AuthenticateAndCreateClient(serverId);
                if (client.conn == null)
                {
                    return (false, "Authentication failed");
                }
                var allLabs = await GetLabs(serverId);
                if (allLabs.labs != null)
                {
                    foreach (var lab in allLabs.labs)
                    {
                        if (lab.Id == labId && lab.State.ToLower() == "started")
                            return (true, "");
                        else if (lab.State.ToLower() == "started")
                            return (false, "Another lab is already running..");
                    }
                }
                var result = await _labApi.StartStopLab(client.conn, labId);
                return (result.result, result.message.ToString());
            }
            catch (Exception e)
            {
                logger.LogError("ApiCiscoLabService - StartLab - " + e.Message);
                return (false, "Something went wrong.. Contact admin.");
            }
        }
        /// <summary>
        /// Stops the specified lab on the Cisco CML server.
        /// </summary>
        /// <param name="serverId">The ID of the server hosting the lab.</param>
        /// <param name="labId">The unique identifier of the lab to stop.</param>
        /// <returns>A tuple indicating success and a message string.</returns>
        public async Task<(bool value, string message)> StopLab(int serverId, string labId)
        {
            try
            {
                var client = await _authService.AuthenticateAndCreateClient(serverId);
                if (client.conn == null)
                {
                    return (false, "Authentication failed");
                }
                var result = await _labApi.StartStopLab(client.conn, labId, false);
                return (result.result, result.message.ToString());
            }
            catch (Exception e)
            {
                logger.LogError("ApiCiscoLabService - StopLab - " + e.Message);
                return (false, "Something went wrong.. Contact admin.");
            }
        }
        /// <summary>
        /// Stops all currently running labs on the given server.
        /// </summary>
        /// <param name="serverId">The ID of the server.</param>
        /// <returns>A tuple indicating whether all labs were successfully stopped and a message string.</returns>
        public async Task<(bool value, string message)> StopAllLabs(int serverId)
        {
            try
            {
                var allLabs = await GetLabs(serverId);
                if (allLabs.labs != null)
                {
                    foreach (var item in allLabs.labs)
                    {
                        if (item.State.ToLower() == "started")
                        {
                            var res = await StopLab(serverId, item.Id);
                            if (!res.value)
                                return (false, res.message);
                        }
                    }
                    return (true, "All labs stopped");
                }
                else
                    return (false, "No labs on this server");
            }
            catch (Exception e)
            {
                logger.LogError("ApiCiscoLabService - StopAllLabs - " + e.Message);
                return (false, "Something went wrong.. Contact admin.");
            }
        }
        /// <summary>
        /// Imports a lab from a raw byte array into the Cisco CML server.
        /// </summary>
        /// <param name="serverId">The ID of the server to import the lab into.</param>
        /// <param name="fileContent">The lab file contents as a byte array.</param>
        /// <returns><c>true</c> if the import was successful; otherwise, <c>false</c>.</returns>
        public async Task<bool> ImportLab(int serverId, byte[] fileContent)
        {
            try
            {
                var client = await _authService.AuthenticateAndCreateClient(serverId);
                if (client.conn == null)
                {
                    return false;
                }
                var result = await _labApi.ImportLab(client.conn, Encoding.UTF8.GetString(fileContent));
                return result;
            }
            catch (Exception e)
            {
                logger.LogError("ApiCiscoLabService - ImportLab - " + e.Message);
                return false;
            }
        }
        /// <summary>
        /// Imports a lab from an uploaded file into the Cisco CML server.
        /// </summary>
        /// <param name="serverId">The ID of the server to import the lab into.</param>
        /// <param name="file">The lab file to import.</param>
        /// <returns><c>true</c> if the import was successful; otherwise, <c>false</c>.</returns>
        public async Task<bool> ImportLab(int serverId, IFormFile file)
        {
            try
            {
                var client = await _authService.AuthenticateAndCreateClient(serverId);
                if (client.conn == null)
                {
                    return false;
                }
                string fileContent;
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    fileContent = await reader.ReadToEndAsync();
                }
                var result = await _labApi.ImportLab(client.conn, fileContent);
                return result;
            }
            catch (Exception e)
            {
                logger.LogError("ApiCiscoLabService - ImportLab - " + e.Message);
                return false;
            }
        }
        /// <summary>
        /// Gets the current running state of the specified lab.
        /// </summary>
        /// <param name="serverId">The ID of the server.</param>
        /// <param name="labId">The unique identifier of the lab.</param>
        /// <returns>The state of the lab as a string, or <c>null</c> if the operation fails.</returns>
        public async Task<string?> GetState(int serverId, string labId)
        {
            try
            {
                var client = await _authService.AuthenticateAndCreateClient(serverId);
                if (client.conn == null)
                {
                    return null;
                }
                var labState = await _labApi.StateOfLab(client.conn, labId);
                if(labState == null)
                    return null;
                return labState.Replace("\"","");
            }
            catch (Exception e)
            {
                logger.LogError("ApiCiscoLabService - GetState - " + e.Message);
                return null;
            }
        }
        /// <summary>
        /// Deletes the specified lab from the Cisco CML server.
        /// </summary>
        /// <param name="serverId">The ID of the server.</param>
        /// <param name="labId">The unique identifier of the lab to delete.</param>
        /// <returns>A tuple indicating whether the lab was successfully deleted and a message string.</returns>
        public async Task<(bool value, string message)> DeleteLab(int serverId, string labId)
        {
            try
            {
                var client = await _authService.AuthenticateAndCreateClient(serverId);
                if (client.conn == null)
                {
                    return (false, "Authentication failed");
                }
                var result = await _labApi.DeleteLab(client.conn, labId);
                if (result.IsSuccessStatusCode)
                {
                    return (true, "Lab deleted successfully");
                }
                else
                {
                    logger.LogError($"ApiCiscoLabService - DeleteLab - {result.StatusCode}");
                    return (false, "Failed to delete lab");
                }
            }
            catch (Exception e)
            {
                logger.LogError("ApiCiscoLabService - DeleteLab - " + e.Message);
                return (false, "Something went wrong.. Contact admin.");
            }
        }
    }
}
