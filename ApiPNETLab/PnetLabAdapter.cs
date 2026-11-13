using System.Net;
using BusinessLayer.DTOs;
using BusinessLayer.Enum;
using BusinessLayer.Interface;
using Microsoft.AspNetCore.Http;
using ApiPNETLab.Client;
using BusinessLayer.Services;
using SimpleLogger;

namespace ApiPNETLab;

public class PnetLabAdapter : IVirtualizationAdapter
{
    private readonly ApiPNETAuthentication _authentication;
    //private readonly ApiPNETLab _apiLab;
    //private readonly ApiPNETNode _apiLabNode;
    private ApiPNETHttpClient? _httpClient;

    /// <summary>
    /// ID of the server the current <c>_httpClient</c> is authenticated to (null = not authenticated).
    /// One adapter instance can be used for several servers within one scope (e.g. BackgroundTask, My labs),
    /// so every call must make sure the client belongs to the requested server.
    /// </summary>
    private int? _authenticatedServerId;
    
    private readonly ILogger _logger = FileLogger.Instance;
    
    public PlatformType PlatformName => PlatformType.PNETLab;
    
    public PnetLabAdapter()
    {
        _authentication = new ApiPNETAuthentication();
        //_apiLab = new ApiPNETLab();
        //_apiLabNode = new ApiPNETNode();
    }
    public async Task<(bool Valid, string Message)> TestConnection(string ipAddress, string username, string password)
    {
        try
        {
            using var httpClient = new ApiPNETHttpClient(ipAddress);
            var response = await _authentication.Authenticate(httpClient, username, password);
            if (response.Success)
            {
                return (true, "Connection successful");
            }
            return (false, response.Message);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError($"PnetLabAdapter - TestConnection - {e.Message}");
            return (false, "Server Unavailable");
        }
        catch (TaskCanceledException e)
        {
            _logger.LogError($"PnetLabAdapter - TestConnection - {e.Message}");
            return (false, "Request Timeout");
        }
        catch (Exception e)
        {
            _logger.LogError($"PnetLabAdapter - TestConnection - Unknown error - {e.Message}");
            return (false, "Unknown error..");
        }
    }

    public async Task<(bool Valid, string Message)> AuthenticateAsync(int id)
    {
        try
        {
            _authenticatedServerId = null;
            var server = new ServerService().GetServerCredentials(id);
            if (server == null)
            {
                _logger.LogWarning($"ApiPNETAuthService - Server with ID {id} not found.");
                return (false, "Server not found.");
            }
            _httpClient?.Dispose();
            _httpClient = new ApiPNETHttpClient(server.Value.Url);
            var response = await _authentication.Authenticate(_httpClient, server.Value.Username, server.Value.Password);
            if (response.Success)
            {
                _authenticatedServerId = id;
                return (true, "OK");
            }
            _logger.LogWarning($"ApiPNETAuthService - Authentication failed ({response.StatusCode}): {response.Message}");
            return (false, response.Message);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError($"ApiPNETAuthService - {e.Message}");
            return (false, "Server Unavailable");
        }
        catch (TaskCanceledException e)
        {
            _logger.LogError($"ApiPNETAuthService - {e.Message}");
            return (false, "Request Timeout..");
        }
        catch (Exception e)
        {
            _logger.LogError($"ApiPNETAuthService - {e.Message}");
            return (false, "Internal server error. Contact admin.");
        }
    }

    public Task<(List<LabDTO>? Labs, string Message)> GetLabsAsync(int serverId)
    {
        throw new NotImplementedException();
    }

    public Task<(LabDTO? Lab, string Message)> GetLabInfoAsync(int serverId, string labId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ImportLab(int serverId, byte[] fileContent, string? filename = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ImportLab(int serverId, IFormFile file)
    {
        throw new NotImplementedException();
    }

    public Task<(byte[]? FileContent, string ContentType, string FileName, string Message)> DownloadLab(int serverId, string? labId, LabDTO? lab = null)
    {
        throw new NotImplementedException();
    }

    public Task<(bool value, string message)> DeleteLab(int serverId, string labId)
    {
        throw new NotImplementedException();
    }

    public Task<string?> StateOfLab(int serverId, string labId)
    {
        throw new NotImplementedException();
    }

    public Task<(bool value, string message)> StartLabAsync(int serverId, string labId)
    {
        throw new NotImplementedException();
    }

    public Task<(bool value, string message)> StopLabAsync(int serverId, string labId)
    {
        throw new NotImplementedException();
    }

    public Task<(List<NodeDTO>? Nodes, string Message)> GetAllNodes(int serverId, string labId)
    {
        throw new NotImplementedException();
    }
    
    public Task ConnectNodeConsoleAsync(
        int serverId,
        string labId,
        string nodeId,
        System.Net.WebSockets.WebSocket browserWs,
        int line = 0,
        uint cols = 120,
        uint rows = 32,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}