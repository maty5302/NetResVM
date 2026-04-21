using ApiCisco.Client;
using ApiCisco.Model;
using BusinessLayer.DTOs;
using BusinessLayer.Enum;
using BusinessLayer.Interface;
using BusinessLayer.Models;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Http;
using SimpleLogger;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ApiCisco;
/// <summary>
/// Provides an adapter for interacting with Cisco CML (Cisco Modeling Labs) servers, enabling management of labs and
/// related resources through a virtualization abstraction.
/// </summary>
/// <remarks>This adapter implements the IVirtualizationAdapter interface to support operations such as
/// authentication, lab import/export, retrieval, and lifecycle management on Cisco CML platforms. It handles server
/// communication, error logging, and result messaging for integration scenarios. Thread safety is not guaranteed;
/// concurrent use should be externally synchronized if required.</remarks>
public class CiscoCmlAdapter : IVirtualizationAdapter 
{
    private readonly ApiCiscoAuthentication _authentication;
    private ApiCiscoHttpClient? _httpClient;
    private readonly ApiCiscoLab _ciscoLab;
    private readonly ApiCiscoNode _node;

    private readonly ILogger _logger = FileLogger.Instance;

    /// <summary>
    /// Gets the platform type associated with this instance.
    /// </summary>
    public PlatformType PlatformName => PlatformType.CML;

    /// <summary>
    /// Initializes a new instance of the CiscoCmlAdapter class with default dependencies for authentication, HTTP
    /// communication, lab management, and node management.
    /// </summary>
    /// <remarks>This constructor sets up the adapter with default implementations for interacting with Cisco
    /// CML APIs. Use this constructor when custom dependency injection is not required.</remarks>
    public CiscoCmlAdapter()
    {
        _authentication = new ApiCiscoAuthentication();
        _httpClient = null;
        _ciscoLab = new ApiCiscoLab();
        _node = new ApiCiscoNode();
    }
    /// <summary>
    /// Asynchronously tests the connection and authentication credentials against a Cisco CML server.
    /// </summary>
    /// <param name="ipAddress">The IP address or base URL of the target CML server.</param>
    /// <param name="username">The username used to authenticate with the server.</param>
    /// <param name="password">The password used to authenticate with the server.</param>
    /// <returns>
    /// A tuple containing a boolean <c>Valid</c> flag indicating whether the connection and authentication were successful, 
    /// and a <c>Message</c> string providing descriptive feedback or specific error details based on the HTTP response.
    /// </returns>
    /// <returns></returns>
    public async Task<(bool Valid, string Message)> TestConnection(string ipAddress, string username, string password)
    {
        try
        {
            var httpClient = new ApiCiscoHttpClient(ipAddress);
            var response = await _authentication.Authenticate(httpClient, username, password);
            if (response.StatusCode == HttpStatusCode.OK)
                return (true, "Connection successful");
            else if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                return (false, "Invalid credentials");
            else if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                return (false, "Service Unavailable");
            else if (response.StatusCode == HttpStatusCode.RequestTimeout)
                return (false, "Request Timeout");
            else
                return (false, "Unknown error..");
        }
        catch (Exception e)
        {
            _logger.LogError($"CiscoCmlAdapter - TestConnection - Unknown error - {e.Message}");
            return (false, "Unknown error..");
        }
    }
    
    /// <summary>
    /// Asynchronously attempts to authenticate to the server identified by the specified ID and returns the result of
    /// the authentication attempt.
    /// </summary>
    /// <remarks>If the server credentials are not found or the authentication fails due to invalid
    /// credentials, service unavailability, or a timeout, the method returns <see langword="false"/> with an
    /// appropriate message. The method logs errors and warnings for various failure scenarios.</remarks>
    /// <param name="id">The unique identifier of the server for which authentication should be performed.</param>
    /// <returns>A tuple containing a Boolean value that is <see langword="true"/> if authentication succeeds; otherwise, <see
    /// langword="false"/>. The accompanying string provides a message describing the result, such as an error or status
    /// message.</returns>
    public async Task<(bool Valid, string Message)> AuthenticateAsync(int id)
    {
        try
        {
            var credentials = new ServerService().GetServerCredentials(id);
            if (credentials == null)
            {
                _logger.LogError($"CiscoCmlAdapter - No credentials found for server with id {id}");
                return (false, "No credentials found for this server");
            }
            _httpClient = new ApiCiscoHttpClient(credentials.Value.Url);
            var response = await _authentication.Authenticate(_httpClient, credentials.Value.Username, credentials.Value.Password);


            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                _logger.LogError("CiscoCmlAdapter - Service Unavailable");
                return (false, "Service Unavailable");
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Unauthorized access. Probably bad password while trying to authenticate to cisco cml server.");
                return (false, "Invalid credentials");
            }
            else if (response.StatusCode == HttpStatusCode.RequestTimeout)
            {
                _logger.LogError("CiscoCmlAdapter - Request Timeout");
                return (false, "Request Timeout");
            }
            else if (response.StatusCode == HttpStatusCode.OK)
                return (true, "");
            else
            {
                _logger.LogError("CiscoCmlAdapter - Unknown error");
                return (false, "Unknown error..");
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"CiscoCmlAdapter - Unknown error - {e.Message}");
            return (false, "Unknown error..");
        }

    }

    /// <summary>
    /// Retrieves detailed information about a specific lab from the Cisco CML server.
    /// </summary>
    /// <param name="labId">The unique identifier of the lab.</param>
    /// <returns>
    /// A tuple containing a <see cref="CiscoLabModel"/> and a message string.
    /// </returns>
    public async Task<(LabDTO? Lab, string Message)> GetLabInfoAsync(int serverId, string labId)
    {
        if (_httpClient == null)
        {
            var authResult = await AuthenticateAsync(serverId);
            if (!authResult.Valid)
            {
                _logger.LogError($"CiscoCmlAdapter - ImportLab - Authentication failed: {authResult.Message}");
                return (null, authResult.Message);
            }
        }

        try
        {
            // Volání tvé API vrstvy pro získání JSONu
            var json = await _ciscoLab.GetLabInfo2(_httpClient, labId);

            if (string.IsNullOrEmpty(json))
            {
                _logger.LogWarning($"CiscoCmlAdapter - Lab not found. ID: {labId}");
                return (null, "Lab not found.");
            }

            // Bezpečná deserializace (jak jsme řešili v předchozím kroku)
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            };

            var ciscoLab = JsonSerializer.Deserialize<CiscoLabModel>(json, options);

            if (ciscoLab == null)
                return (null, "Failed to parse lab data.");

            var labState = ciscoLab.State ?? "Unknown";
            if (labState == "")
                return (null, "Lab state is unknown.");
            else if (labState=="DEFINED_ON_CORE")
            {
                labState = "STOPPED";
            }
            // PŘEVOD CiscoLabModel -> LabDTO
            var dto = new LabDTO
            {
                Id = ciscoLab.Id,
                Name = ciscoLab.Name,
                Description = ciscoLab.Description ?? "",
                Status = ciscoLab.State ?? "Unknown"
            };

            // Uložení specifických CML dat do Metadata slovníku
            dto.Metadata.Add("NodeCount", ciscoLab.Node_count.ToString());
            dto.Metadata.Add("LinkCount", ciscoLab.Link_count.ToString());
            dto.Metadata.Add("LastModified", ciscoLab.Last_modified.ToString() ?? "");

            return (dto, "OK");
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError($"CiscoCmlAdapter - JSON parse error for lab {labId}: {jsonEx.Message}");
            return (null, "API returned unexpected data format.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"CiscoCmlAdapter - GetLabInfoAsync error: {ex.Message}");
            return (null, "Internal error occurred.");
        }
    }
    /// <summary>
    /// Retrieves a list of all available labs on the specified Cisco CML server.
    /// </summary>
    /// <returns>
    /// A tuple containing a list of <see cref="LabDTO"/> and a message string.
    /// </returns>
    public async Task<(List<LabDTO>? Labs, string Message)> GetLabsAsync(int serverId)
    {
        if (_httpClient == null)
        {
            var authResult = await AuthenticateAsync(serverId);
            if (!authResult.Valid)
            {
                _logger.LogError($"CiscoCmlAdapter - GetLabsAsync - Authentication failed: {authResult.Message}");
                return (null, authResult.Message);
            }
        }

        try
        {
            // Get all lab IDs from the API
            var labsIDs = await _ciscoLab.GetLabs(_httpClient);

            if (labsIDs == null)
            {
                _logger.LogError("CiscoCmlAdapter - Couldn't fetch labs IDs from cisco API.");
                return (null, "Couldn't fetch labs from cisco API.");
            }

            var labsList = new List<LabDTO>();

            // Go through each lab ID and fetch detailed info for each lab
            foreach (var id in labsIDs)
            {
                var result = await GetLabInfoAsync(serverId, id);

                if (result.Lab != null)
                {
                    labsList.Add(result.Lab);
                }
            }

            return (labsList, "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError($"CiscoCmlAdapter - GetLabsAsync error: {ex.Message}");
            return (null, "Internal error occurred while fetching labs.");
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
            if (_httpClient == null)
            {
                var authResult = await AuthenticateAsync(serverId);
                if(!authResult.Valid)
                {
                    _logger.LogError($"CiscoCmlAdapter - ImportLab - Authentication failed: {authResult.Message}");
                    return false;
                }
            }
            string fileContent;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                fileContent = await reader.ReadToEndAsync();
            }
            var result = await _ciscoLab.ImportLab(_httpClient, fileContent);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError("CiscoCmlAdapter - ImportLab - " + e.Message);
            return false;
        }
    }
    /// <summary>
    /// Imports a lab from a raw byte array into the Cisco CML server.
    /// </summary>
    /// <param name="serverId">The ID of the server to import the lab into.</param>
    /// <param name="fileContent">The lab file contents as a byte array.</param>
    /// <param name="filename">Optional filename for the lab (currently unused by the implementation).</param>
    /// <returns><c>true</c> if the import was successful; otherwise, <c>false</c>.</returns>
    public async Task<bool> ImportLab(int serverId, byte[] fileContent, string? filename = null)
    {
        try
        {
            if (_httpClient == null)
            {
                var authResult = await AuthenticateAsync(serverId);
                if(!authResult.Valid)
                {
                    _logger.LogError($"CiscoCmlAdapter - ImportLab - Authentication failed: {authResult.Message}");
                    return false;
                }
            }
            string contentString = System.Text.Encoding.UTF8.GetString(fileContent);
            var result = await _ciscoLab.ImportLab(_httpClient, contentString);
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError("CiscoCmlAdapter - ImportLab - " + e.Message);
            return false;
        }
    }
    /// <summary>
    /// Asynchronously downloads a lab from the Cisco CML server in YAML format.
    /// </summary>
    /// <param name="serverId">The ID of the Cisco CML server hosting the lab.</param>
    /// <param name="labId">The unique identifier of the lab to download. Required parameter.</param>
    /// <param name="lab">Optional LabDTO object (currently unused by the implementation).</param>
    /// <returns>
    /// A tuple containing:
    /// - FileContent: Byte array with the lab file content in UTF-8 encoding, or null if an error occurs
    /// - ContentType: MIME type of the file ("text/plain" on success, empty string on error)
    /// - FileName: Name of the downloaded file ("lab.yaml" on success, empty string on error)
    /// - Message: Status message ("" on success, error description on failure)
    /// </returns>
    public async Task<(byte[]? FileContent, string ContentType, string FileName, string Message)> DownloadLab(int serverId, string? labId, LabDTO? lab = null)
    {
        try
        {
            if (_httpClient == null)
            {
                var authResult = await AuthenticateAsync(serverId);
                if (!authResult.Valid)
                {
                    _logger.LogError($"CiscoCmlAdapter - DownloadLab - Authentication failed: {authResult.Message}");
                    return (null, "", "", "Authentication failed");
                }
            }
            if(String.IsNullOrEmpty(labId))
            {
                _logger.LogError("CiscoCmlAdapter - DownloadLab - Lab ID is null or empty.");
                return (null,"","" ,"Lab ID is required.");
            }   

            var data = await _ciscoLab.GetLabInfo2(_httpClient, labId, true);

            if (data == null)
            {
                _logger.LogError("CiscoCmlAdapter - DownloadLab - Lab not found.");  
                return (null, "","", "Lab not found..");
            }
            
            return (Encoding.UTF8.GetBytes(data),"text/plain", "lab.yaml","");
            
        }
        catch (Exception e)
        {
            _logger.LogError("CiscoCmlAdapter - DownloadLab - " + e.Message);
            return (null,"","", "Something went wrong.. Contact admin.");
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
            if (_httpClient == null)
            {
                var authResult = await AuthenticateAsync(serverId);
                if (!authResult.Valid)
                {
                    _logger.LogError($"CiscoCmlAdapter - DownloadLab - Authentication failed: {authResult.Message}");
                    return (false, "Authentication failed");
                }
            }
            var result = await _ciscoLab.DeleteLab(_httpClient, labId);
            if (result.IsSuccessStatusCode)
            {
                return (true, "Lab deleted successfully");
            }
            else
            {
                _logger.LogError($"CiscoCmlAdapter - DeleteLab - {result.StatusCode}");
                return (false, "Failed to delete lab");
            }
        }
        catch (Exception e)
        {
            _logger.LogError("CiscoCmlAdapter - DeleteLab - " + e.Message);
            return (false, "Something went wrong.. Contact admin.");
        }
    }
    /// <summary>
    /// Starts the specified lab if no other lab is currently running.
    /// </summary>
    /// <param name="serverId">The ID of the server hosting the lab.</param>
    /// <param name="labId">The unique identifier of the lab to start.</param>
    /// <returns>A tuple indicating whether the operation was successful and a message string.</returns>
    public async Task<(bool value, string message)> StartLabAsync(int serverId, string labId)
    {
        try
        {
            if (_httpClient == null)
            {
                var authResult = await AuthenticateAsync(serverId);
                if (!authResult.Valid)
                {
                    _logger.LogError($"CiscoCmlAdapter - StartLabAsync - Authentication failed: {authResult.Message}");
                    return (false, "Authentication failed");
                }
            }
            var allLabs = await GetLabsAsync(serverId);
            if (allLabs.Labs != null)
            {
                foreach (var lab in allLabs.Labs)
                {
                        if (lab.Id == labId && lab.Status.ToLower() == "started")
                            return (true, "");
                        else if (lab.Status.ToLower() == "started")
                            return (false, "Another lab is already running..");
                }
            }
            var result = await _ciscoLab.StartStopLab(_httpClient, labId);
            return (result.result, result.message.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError("CiscoCmlAdapter - StartLab - " + e.Message);
            return (false, "Something went wrong.. Contact admin.");
        }
    }
    /// <summary>
    /// Stops the specified lab on the Cisco CML server.
    /// </summary>
    /// <param name="serverId">The ID of the server hosting the lab.</param>
    /// <param name="labId">The unique identifier of the lab to stop.</param>
    /// <returns>A tuple indicating success and a message string.</returns>
    public async Task<(bool value, string message)> StopLabAsync(int serverId, string labId)
    {
        try
        {
            if (_httpClient == null)
            {
                var authResult = await AuthenticateAsync(serverId);
                if (!authResult.Valid)
                {
                    _logger.LogError($"CiscoCmlAdapter - StopLabAsync - Authentication failed: {authResult.Message}");
                    return (false, "Authentication failed");
                }
            }
            var result = await _ciscoLab.StartStopLab(_httpClient, labId, false);
            return (result.result, result.message.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError("ApiCiscoLabService - StopLab - " + e.Message);
            return (false, "Something went wrong.. Contact admin.");
        }
    }
    /// <summary>
    /// Retrieves all nodes associated with the specified lab on the given server.
    /// </summary>
    /// <remarks>If authentication fails or an error occurs during retrieval, the returned list will be null
    /// and the message will contain details about the failure. The method logs errors for failed authentication and
    /// retrieval attempts.</remarks>
    /// <param name="serverId">The identifier of the server from which to retrieve nodes. Must correspond to a valid, accessible server.</param>
    /// <param name="labId">The unique identifier of the lab whose nodes are to be retrieved. Cannot be null or empty.</param>
    /// <returns>A tuple containing a list of node data transfer objects for the specified lab, or null if retrieval fails, and a
    /// message describing the result or any error encountered.</returns>
    public async Task<(List<NodeDTO>? Nodes, string Message)> GetAllNodes(int serverId, string labId)
    {
        if (_httpClient == null)
        {
            var authResult = await AuthenticateAsync(serverId);
            if (!authResult.Valid)
            {
                _logger.LogError($"CiscoCmlAdapter - StopLabAsync - Authentication failed: {authResult.Message}");
                return (null, "Authentication failed");
            }
        }

        try
        {
            var response = await _node.GetNodes(_httpClient, labId);
            if (response == null)
            {
                _logger.LogError($"ApiCiscoNodeService - Couldn't retrieve nodes from a lab {labId}..");
                return (null, "Couldn't retrieve nodes from the lab");
            }
            var nodes = new List<NodeDTO>();
            foreach (var node in response)
            {
                var deserNode = await GetNodeInfo(serverId, labId, node);
                if (deserNode != null)
                    nodes.Add(deserNode);
            }
            return (nodes, "");
        }
        catch (Exception e)
        {
            _logger.LogError($"ApiCiscoNodeService - Couldn't retrieve nodes from a lab.. {e.Message}");
            return (null, "Something went wrong.. Couldn't retrieve nodes from a lab. Contact admin.");
        }
    }
    /// <summary>
    /// Retrieves detailed information about a specific node within a lab on the specified server.
    /// </summary>
    /// <remarks>If authentication fails or the node cannot be found, the method returns null. The returned
    /// NodeDTO includes metadata such as CPU limit and data volume.</remarks>
    /// <param name="serverId">The identifier of the server from which to retrieve node information.</param>
    /// <param name="labId">The unique identifier of the lab containing the node.</param>
    /// <param name="nodeId">The unique identifier of the node whose information is to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a NodeDTO with the node's details if
    /// found; otherwise, null.</returns>
    public async Task<NodeDTO?> GetNodeInfo(int serverId, string labId, string nodeId)
    {
        if (_httpClient == null)
        {
            var authResult = await AuthenticateAsync(serverId);
            if (!authResult.Valid)
            {
                _logger.LogError($"CiscoCmlAdapter - StopLabAsync - Authentication failed: {authResult.Message}");
                return null;
            }
        }

        var response = await _node.GetNodeInfo(_httpClient, labId, nodeId);
        if (response == null)
        {
            _logger.LogWarning($"ApiCiscoNodeService - Couldn't retrieve node info {nodeId} from a lab {labId}");
            return null;
        }

        var node = JsonSerializer.Deserialize<CiscoNodeModel>(response);
        if (node == null)
            return null;

        var nodeDTO = new NodeDTO
        {
            Id = node.Id,
            Name = node.Name,
            Status = node.State,
            NumberOfCPU = node.Cpu ?? 0,
            Memory = node.Memory ?? 0,
        };
        nodeDTO.Metadata.Add("CPU_Limit", node.CpuLimit?.ToString() ?? "0");
        nodeDTO.Metadata.Add("DataVolume", node.DataVolume?.ToString() ?? "0");

        return nodeDTO;
    }

    /// <summary>
    /// Retrieves the current state of the specified lab from the server asynchronously.
    /// </summary>
    /// <remarks>If authentication fails or the lab state cannot be retrieved, the method returns null and
    /// logs an error. The returned state string has any quotation marks removed.</remarks>
    /// <param name="serverId">The identifier of the server to connect to for retrieving the lab state.</param>
    /// <param name="labId">The unique identifier of the lab whose state is to be retrieved.</param>
    /// <returns>A string representing the current state of the lab, or null if the state could not be retrieved.</returns>
    public async Task<string?> StateOfLab(int serverId, string labId)
    {
        try
        {
            if (_httpClient == null)
            {
                var authResult = await AuthenticateAsync(serverId);
                if (!authResult.Valid)
                {
                    _logger.LogError($"CiscoCmlAdapter - StopLabAsync - Authentication failed: {authResult.Message}");
                    return null;
                }
            }
            var labState = await _ciscoLab.StateOfLab(_httpClient, labId);
            if(labState == null)
            {                 
                _logger.LogError($"CiscoCmlAdapter - StateOfLab - Couldn't retrieve state of lab {labId}.");
                return null;
            }
            return labState.Replace("\"", "");

        }
        catch (Exception e)
        {
            _logger.LogError($"CiscoCmlAdapter - StateOfLab - Error: {e.Message}");
            return null;
        }
    }
}