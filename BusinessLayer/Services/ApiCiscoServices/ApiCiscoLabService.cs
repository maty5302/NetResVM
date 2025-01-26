using ApiCisco;
using SimpleLogger;
using BusinessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BusinessLayer.Services.ApiCiscoServices
{
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

        public async Task<(CiscoLabModel? lab, string Message)> GetLabInfo(int serverId, string labId, UserHttpClient? httpClient = null)
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
    }
}
