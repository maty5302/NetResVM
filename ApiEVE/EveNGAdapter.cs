using ApiEVE.Client;
using ApiEVE.Models;
using BusinessLayer.DTOs;
using BusinessLayer.Enum;
using BusinessLayer.Interface;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Http;
using SimpleLogger;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
namespace ApiEVE;

public class EveNGAdapter : IVirtualizationAdapter
{
    private readonly ApiEVEAuthentication _authentication;
    private readonly ApiEVELab _apiLab;
    private readonly ApiEVENode _apiLabNode;
    private ApiEVEHttpClient? _httpClient;

    private readonly ILogger _logger = FileLogger.Instance;
    public PlatformType PlatformName => PlatformType.EVE;

    public EveNGAdapter()
    {
        _authentication = new ApiEVEAuthentication();
        _apiLab = new ApiEVELab();
        _apiLabNode = new ApiEVENode();
    }

    /// <summary>
    /// Asynchronously attempts to authenticate with the server identified by the specified ID using stored credentials.
    /// </summary>
    /// <remarks>If the server with the specified ID is not found, authentication fails with an appropriate
    /// message. Network errors and timeouts are handled and reported in the result message. This method logs warnings
    /// and errors for failed authentication attempts and exceptions.</remarks>
    /// <param name="id">The unique identifier of the server whose credentials are used for authentication.</param>
    /// <returns>A tuple where the first value indicates whether authentication was successful, and the second value contains a
    /// message describing the result. Returns (<see langword="true"/>, "OK") if authentication succeeds; otherwise,
    /// returns (<see langword="false"/>, error message) describing the failure reason.</returns>
    public async Task<(bool Valid, string Message)> AuthenticateAsync(int id)
    {
        try
        {
            var server = new ServerService().GetServerCredentials(id);
            if (server == null)
            {
                _logger.LogWarning($"ApiEVEAuthService - Server with ID {id} not found.");
                return (false, "Server not found.");
            }
            _httpClient = new ApiEVEHttpClient(server.Value.Url); // Create a new instance of ApiEVEHttpClient with the server's IP address
            var response = await _authentication.Authenticate(_httpClient, server.Value.Username, server.Value.Password); // Authenticate the user with the server's credentials
            if (response.IsSuccessStatusCode)
                return (true, "OK");

            _logger.LogWarning($"ApiEVEAuthService - Invalid credentials - {response.StatusCode.ToString()}");
            return (false, $"Invalid credentials. Response: {response.StatusCode.ToString()}");
        }
        catch (HttpRequestException e)
        {
            _logger.LogError($"ApiEVEAuthService - {e.Message}");
            return (false, "Server Unavailable");
        }
        catch (TaskCanceledException e)
        {
            _logger.LogError($"ApiEVEAuthService - {e.Message}");
            return (false, "Request Timeout..");
        }
        catch (Exception e)
        {
            _logger.LogError($"ApiEVEAuthService - {e.Message}");
            return (false, "Internal server error. Contact admin.");
        }
    }
    
    /// <summary>
    /// Deletes the specified lab from the server identified by the given server ID.
    /// </summary>
    /// <remarks>If authentication is required and fails, the method returns <see langword="false"/> with an
    /// appropriate error message. The returned message can be used for logging or user feedback.</remarks>
    /// <param name="serverId">The unique identifier of the server on which the lab resides.</param>
    /// <param name="labId">The unique identifier of the lab to delete.</param>
    /// <returns>A tuple containing a Boolean value that is <see langword="true"/> if the lab was deleted successfully;
    /// otherwise, <see langword="false"/>. The accompanying string provides a message describing the result.</returns>
    public async Task<(bool value, string message)> DeleteLab(int serverId, string labId)
    {
        if (_httpClient == null)
        {
            var authResult = await AuthenticateAsync(serverId);
            if (!authResult.Valid)
            {
                _logger.LogError($"EveAdapter - DeleteLab - Authentication failed: {authResult.Message}");
                return (false, authResult.Message);
            }
        }
        try
        {
            var res = await _apiLab.DeleteLab(_httpClient, labId);
            return (res, "Success");
        }
        catch (Exception e)
        {
            _logger.LogError($"ApiEVELabService - DeleteLab - {e.Message}");
            return (false, "Failed to delete lab.");
        }
    }

    /// <summary>
    /// Downloads the specified lab as a ZIP archive from the server.
    /// </summary>
    /// <remarks>The method returns a ZIP archive containing the lab file if the operation is successful. If
    /// required metadata is missing or authentication fails, the method returns an error message and no file
    /// content.</remarks>
    /// <param name="serverId">The identifier of the server from which to download the lab.</param>
    /// <param name="labId">The unique identifier of the lab to download. Can be null if lab information is provided in <paramref
    /// name="lab"/>.</param>
    /// <param name="lab">An optional <see cref="LabDTO"/> object containing metadata about the lab. If null, the method will not proceed
    /// with the download.</param>
    /// <returns>A tuple containing the file content as a byte array, the MIME content type, the file name, and a message
    /// indicating the result. If the download fails, <c>FileContent</c> will be null and the message will describe the
    /// error.</returns>
    public async Task<(byte[]? FileContent, string ContentType, string FileName, string Message)> DownloadLab(int serverId, string? labId, LabDTO? lab = null)
    {
        if (_httpClient == null)
        {
            var authResult = await AuthenticateAsync(serverId);
            if (!authResult.Valid)
            {
                _logger.LogError($"EveAdapter - DownloadLab - Authentication failed: {authResult.Message}");
                return (null, "", "", authResult.Message);
            }
        }
        if(lab == null || labId ==null)
        {
            _logger.LogError($"EveAdapter - DownloadLab - LabDTO is null for labId {labId}");
            return (null, "", "", "Lab information is missing.");
        }
        //var labPath = lab.Metadata.ContainsKey("Path") ? lab.Metadata["Path"] : null;
        var labFilename = lab.Metadata.ContainsKey("Filename") ? lab.Metadata["Filename"] : null;
        if (labFilename == null)
        {
            _logger.LogError($"EveAdapter - DownloadLab - Lab path or filename not found in metadata for lab {lab.Name}");
            return (null, "", "", "Lab path or filename not found in metadata.");
        }
        string pathOnly = new string(labId.Except(labFilename).ToArray());
        var obj = new Dictionary<string, string>
        {
            { "\"0\"", labId },
            { "path", pathOnly }
        };
        string jsonData = JsonSerializer.Serialize(obj);
        var file = await _apiLab.ExportLab(_httpClient, jsonData);
        if (file != null)
        {
            using (var ms = new MemoryStream())
            {
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    var entry = archive.CreateEntry(labFilename);
                    using (var entryStream = entry.Open())
                    {
                        using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                        {
                            writer.Write(file);
                        }
                    }
                }
                return (ms.ToArray(), "application/zip", labFilename, "Success");
            }
        }
        return (null, "", "", "Lab not found.");
    }

    public async Task<(List<NodeDTO>? Nodes, string Message)> GetAllNodes(int serverId, string labId)
    {
        if (_httpClient == null)
        {
            var authResult = await AuthenticateAsync(serverId);
            if (!authResult.Valid)
            {
                _logger.LogError($"EveAdapter - GetAllNodes - Authentication failed: {authResult.Message}");
                return (null, authResult.Message);
            }
        }
        var nodes = await _apiLabNode.GetAllNodes(_httpClient, labId);
        if (nodes.nodes == null)
        {
            _logger.LogError($"ApiEVENodeService - GetAllNodes - {nodes.code}");
            return (null, "Failed to retrieve nodes.");
        }
        var jsonDoc = JsonDocument.Parse(nodes.nodes);
        var labsElement = jsonDoc.RootElement.GetProperty("data");
        List<NodeDTO> allNodes = new List<NodeDTO>();
        foreach (var lab in labsElement.EnumerateObject())
        {
            var res = JsonSerializer.Deserialize<EVENodeModel>(lab.Value);
            if (res == null)
                continue;

            var nodeStatus = res.Status == 2 ? "Running" : "Stopped";
            var nodeDto = new NodeDTO
            {
                Id = res.Id.ToString(),
                Name = res.Name,
                Status = nodeStatus,
                NumberOfCPU = res.NumberOfCPU,
                Memory = res.Memory,
                Metadata = new Dictionary<string, string>
                {
                    { "NumberOfEthernet", res.NumberOfEthernet.ToString() },
                    { "UrlConnect", res.UrlConnect }
                }
            };

            allNodes.Add(nodeDto);
        }
        return (allNodes, "Success");
    }

    /// <summary>
    /// Asynchronously retrieves information about a specific lab from the EVE-NG server.
    /// </summary>
    /// <remarks>If authentication is required and fails, the method returns <see langword="null"/> for the
    /// lab and an error message. The returned message provides additional context, such as error details or success
    /// status. The method does not throw exceptions for common error conditions; instead, it returns descriptive
    /// messages in the result tuple.</remarks>
    /// <param name="serverId">The identifier of the EVE-NG server from which to retrieve the lab information.</param>
    /// <param name="labId">The unique identifier or path of the lab to retrieve.</param>
    /// <returns>A tuple containing a <see cref="LabDTO"/> with the lab details if found, or <see langword="null"/> if not found,
    /// and a message describing the result.</returns>
    public async Task<(LabDTO? Lab, string Message)> GetLabInfoAsync(int serverId, string labId)
    {
        try
        {
            // 1. Kontrola autentizace
            if (_httpClient == null)
            {
                var authResult = await AuthenticateAsync(serverId);
                if (!authResult.Valid)
                {
                    _logger.LogError($"EveAdapter - GetLabInfoAsync - Authentication failed: {authResult.Message}");
                    return (null, authResult.Message);
                }
            }

            // 2. Stažení dat z EVE-NG API 
            // (Předpokládám, že máš v _apiLab metodu, která umí stáhnout info o 1 laboratoři podle ID/cesty)
            var labResponse = await _apiLab.GetLabInfo(_httpClient, labId);

            if (string.IsNullOrEmpty(labResponse))
            {
                _logger.LogWarning($"EveAdapter - GetLabInfoAsync - Lab {labId} not found.");
                return (null, "Lab not found.");
            }

            // 3. Parsování JSONu
            using var jsonDoc = JsonDocument.Parse(labResponse);

            // EVE-NG obvykle vrací data uvnitř objektu "data"
            if (!jsonDoc.RootElement.TryGetProperty("data", out var dataElement))
            {
                _logger.LogError("EveAdapter - GetLabInfoAsync - Invalid JSON structure (missing 'data' node).");
                return (null, "Invalid data format received from server.");
            }

            // 4. Sestavení univerzálního DTO
            var labDto = new LabDTO
            {
                // Pokud EVE nevrátí ID, použijeme jako identifikátor vstupní labId (cestu k souboru)
                Id = labId,
                Name = dataElement.TryGetProperty("name", out var nameProp) ? (nameProp.GetString() ?? "Unknown") : "Unknown",
                Description = dataElement.TryGetProperty("description", out var descProp) ? descProp.GetString() : "",

                //TO-DO předělat
                // EVE-NG ne vždy u laboratoře vrací stav přímo, pokud ho nemáš, dej zástupnou hodnotu
                Status = dataElement.TryGetProperty("status", out var statProp) ? (statProp.GetString() ?? "UNKNOWN") : "UNKNOWN"
            };

            // 5. Zabalení EVE-NG specifik do slovníku Metadata (aby se vypsaly v LabInfo.cshtml)
            if (dataElement.TryGetProperty("id", out var idProp) && !string.IsNullOrEmpty(idProp.GetString()))
                labDto.Metadata.Add("UUID", idProp.GetString()!);

            if (dataElement.TryGetProperty("path", out var pathProp) && !string.IsNullOrEmpty(pathProp.GetString()))
                labDto.Metadata.Add("Path", pathProp.GetString()!);

            if (dataElement.TryGetProperty("filename", out var fileProp) && !string.IsNullOrEmpty(fileProp.GetString()))
                labDto.Metadata.Add("Filename", fileProp.GetString()!);

            if (dataElement.TryGetProperty("author", out var authorProp) && !string.IsNullOrEmpty(authorProp.GetString()))
                labDto.Metadata.Add("Author", authorProp.GetString()!);

            return (labDto, "Success");
        }
        catch (JsonException ex)
        {
            _logger.LogError($"EveAdapter - GetLabInfoAsync - JSON Parsing Error: {ex.Message}");
            return (null, "Failed to parse lab data from EVE-NG server.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"EveAdapter - GetLabInfoAsync - Unexpected Error: {ex.Message}");
            return (null, "An unexpected error occurred while fetching lab info.");
        }
    }

    /// <summary>
    /// Asynchronously retrieves the list of labs available on the specified EVE-NG server.
    /// </summary>
    /// <remarks>If authentication with the server fails or the server returns invalid or empty data, the
    /// method returns null for the labs list and provides an explanatory message. The returned message can be used for
    /// error handling or user feedback.</remarks>
    /// <param name="serverId">The unique identifier of the EVE-NG server from which to retrieve labs.</param>
    /// <returns>A tuple containing a list of lab data transfer objects if successful, or null if an error occurs, and a message
    /// describing the result or any error encountered.</returns>
    public async Task<(List<LabDTO>? Labs, string Message)> GetLabsAsync(int serverId)
    {
        try
        {
            if (_httpClient == null)
            {
                var authResult = await AuthenticateAsync(serverId);
                if (!authResult.Valid)
                {
                    _logger.LogError($"EveAdapter - GetLabsAsync - Authentication failed: {authResult.Message}");
                    return (null, authResult.Message);
                }
            }

            var labsResponse = await _apiLab.GetLabs(_httpClient);
            if (string.IsNullOrEmpty(labsResponse))
            {
                return (null, "Received empty response from EVE-NG server.");
            }

            using var jsonDoc = JsonDocument.Parse(labsResponse);

            if (!jsonDoc.RootElement.TryGetProperty("data", out var dataElement) ||
                !dataElement.TryGetProperty("labs", out var labsElement))
            {
                _logger.LogError("EveAdapter - GetLabsAsync - Invalid JSON structure. Missing 'data.labs' array.");
                return (null, "Invalid data format received from server.");
            }

            var fileNames = new List<(string FileName, string Path, DateTime LastModified)>();
            string dateFormat = "dd MMM yyyy HH:mm";

            foreach (var lab in labsElement.EnumerateArray())
            {
                string? file = lab.TryGetProperty("file", out var f) ? f.GetString() : null;
                string? path = lab.TryGetProperty("path", out var p) ? p.GetString() : null;
                string? mtimeString = lab.TryGetProperty("mtime", out var m) ? m.GetString() : null;

                if (string.IsNullOrEmpty(file) || string.IsNullOrEmpty(path))
                {
                    continue;
                }

                DateTime lastModifiedDate = DateTime.MinValue;
                if (!string.IsNullOrEmpty(mtimeString))
                {
                    DateTime.TryParseExact(mtimeString, dateFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out lastModifiedDate);
                }

                fileNames.Add((file, path, lastModifiedDate));
            }

            var labModels = new List<LabDTO>();

            // --- TADY JE TA HLAVNÍ ZMĚNA ---
            foreach (var item in fileNames)
            {
                // 1. Sestavíme labId pro EVE-NG (spojení cesty a názvu souboru)
                //// Např. path: "/", file: "test.unl" -> labId: "/test.unl"
                //string labId = item.Path == "/" ? $"/{item.FileName}" : $"{item.Path}/{item.FileName}";

                // 2. Zavoláme tvou novou metodu GetLabInfoAsync
                var labResult = await GetLabInfoAsync(serverId, item.Path);

                if (labResult.Lab != null)
                {
                    // VYLEPŠENÍ: Z detailu laboratoře EVE často nevrací datum úpravy. 
                    // Protože ho ale známe z hlavního seznamu, rovnou ho do Metadata doplníme!
                    if (!labResult.Lab.Metadata.ContainsKey("LastModified") && item.LastModified != DateTime.MinValue)
                    {
                        labResult.Lab.Metadata["LastModified"] = item.LastModified.ToString(dateFormat);
                    }

                    labModels.Add(labResult.Lab);
                }
            }

            return (labModels, "Success");
        }
        catch (JsonException ex)
        {
            _logger.LogError($"EveAdapter - GetLabsAsync - JSON Parsing Error: {ex.Message}");
            return (null, "Failed to parse data from EVE-NG server.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"EveAdapter - GetLabsAsync - Unexpected Error: {ex.Message}");
            return (null, "An unexpected error occurred while fetching labs.");
        }
    }

    /// <summary>
    /// Imports a lab file to the specified server.
    /// </summary>
    /// <remarks>If authentication is required and fails, the method logs an error and returns <see
    /// langword="false"/>. Errors during import are also logged and result in a <see langword="false"/> return
    /// value.</remarks>
    /// <param name="serverId">The identifier of the server to which the lab will be imported.</param>
    /// <param name="fileContent">The binary content of the lab file to import. Cannot be null.</param>
    /// <param name="filename">The name of the lab file. If null, a default name may be used.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the import
    /// succeeds; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> ImportLab(int serverId, byte[] fileContent, string? filename = null)
    {
        if (_httpClient == null)
        {
            var authResult = await AuthenticateAsync(serverId);
            if (!authResult.Valid)
            {
                _logger.LogError($"EveNGAdapter - ImportLab - Authentication failed: {authResult.Message}");
                return false;
            }
        }
        try
        {
            return await _apiLab.ImportLab(_httpClient, fileContent, filename);
        }
        catch (Exception e)
        {
            _logger.LogError($"EveNGAdapter - ImportLab - {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Imports a lab file to the specified server asynchronously.
    /// </summary>
    /// <remarks>Authentication is performed automatically if required. The method logs an error and returns
    /// <see langword="false"/> if authentication fails.</remarks>
    /// <param name="serverId">The identifier of the target server to which the lab will be imported.</param>
    /// <param name="file">The lab file to import. Must be a valid, non-null file representing the lab configuration.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the import
    /// succeeds; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> ImportLab(int serverId, IFormFile file)
    {
        if (_httpClient == null)
        {
            var authResult = await AuthenticateAsync(serverId);
            if (!authResult.Valid)
            {
                _logger.LogError($"EveNGAdapter - ImportLab - Authentication failed: {authResult.Message}");
                return false;
            }
        }
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            return await _apiLab.ImportLab(_httpClient, ms.ToArray(), file.FileName);
        }
    }

    /// <summary>
    /// Starts all nodes in the specified lab on the given server asynchronously.
    /// </summary>
    /// <remarks>If authentication fails or no nodes are found in the lab, the operation does not proceed and
    /// returns an appropriate message. If any node fails to start, the method returns a list of the failed nodes in the
    /// message.</remarks>
    /// <param name="serverId">The identifier of the server on which the lab resides.</param>
    /// <param name="labId">The unique identifier of the lab whose nodes are to be started.</param>
    /// <returns>A tuple containing a boolean value that indicates whether all nodes were started successfully, and a message
    /// describing the result.</returns>
    public async Task<(bool value, string message)> StartLabAsync(int serverId, string labId)
    {
        if (_httpClient == null)
        {
            var authResult = await AuthenticateAsync(serverId);
            if (!authResult.Valid)
            {
                _logger.LogError($"EveNGAdapter - StartLabAsync - Authentication failed: {authResult.Message}");
                return (false, authResult.Message);
            }
        }
        var nodes = await GetAllNodes(serverId, labId);
        if (nodes.Nodes == null || nodes.Nodes.Count==0)
        {
            _logger.LogError("EveNGAdapter - StartLabAsync - No nodes found to start.");
            return (false, "No nodes found to start.");
        }
        bool allNodes = true;
        List<string> failedNodes = new List<string>();
        foreach (var node in nodes.Nodes)
        {
            var nodeId = Convert.ToInt32(node.Id);
            var res = await _apiLabNode.StartNode(_httpClient, labId , nodeId);
            if (res!=HttpStatusCode.OK)
            {
                allNodes = false;
                failedNodes.Add(node.Name);
                _logger.LogError($"EveNGAdapter - StartLabAsync - Failed to start node {node.Name} (ID: {node.Id}).");
            }
        }
        if(allNodes)
            return (true, "All nodes started successfully.");
        else
            return (false, $"Failed to start nodes: {string.Join(", ", failedNodes)}");
    }

    /// <summary>
    /// Stops all nodes in the specified lab on the given server asynchronously.
    /// </summary>
    /// <remarks>If authentication fails or no nodes are found in the lab, the operation does not proceed and
    /// returns an appropriate message. If some nodes fail to stop, their names are included in the result
    /// message.</remarks>
    /// <param name="serverId">The identifier of the server hosting the lab.</param>
    /// <param name="labId">The unique identifier of the lab whose nodes are to be stopped.</param>
    /// <returns>A tuple containing a boolean value that is <see langword="true"/> if all nodes were stopped successfully;
    /// otherwise, <see langword="false"/>. The accompanying string provides a message describing the result.</returns>
    public async Task<(bool value, string message)> StopLabAsync(int serverId, string labId)
    {
        if (_httpClient == null)
        {
            var authResult = await AuthenticateAsync(serverId);
            if (!authResult.Valid)
            {
                _logger.LogError($"EveNGAdapter - StopLabAsync - Authentication failed: {authResult.Message}");
                return (false, authResult.Message);
            }
        }
        var nodes = await GetAllNodes(serverId, labId);
        if (nodes.Nodes == null || nodes.Nodes.Count == 0)
        {
            _logger.LogError("EveNGAdapter - StopLabAsync - No nodes found to stop.");
            return (false, "No nodes found to stop.");
        }
        bool allNodes = true;
        List<string> failedNodes = new List<string>();
        foreach (var node in nodes.Nodes)
        {
            var nodeId = Convert.ToInt32(node.Id);
            var res = await _apiLabNode.StopNode(_httpClient, labId, nodeId);
            if (res != HttpStatusCode.OK)
            {
                allNodes = false;
                failedNodes.Add(node.Name);
                _logger.LogError($"EveNGAdapter - StopLabAsync - Failed to stop node {node.Name} (ID: {node.Id}).");
            }
        }
        if (allNodes)
            return (true, "All nodes stopped successfully.");
        else
            return (false, $"Failed to stop nodes: {string.Join(", ", failedNodes)}");
    }

    /// <summary>
    /// Asynchronously tests the connection to the specified server using the provided credentials.
    /// </summary>
    /// <remarks>The returned message provides additional context for the connection result, such as
    /// authentication errors or service availability. This method does not throw exceptions for connection failures;
    /// instead, it returns a descriptive message in the result tuple.</remarks>
    /// <param name="ipAddress">The IP address of the server to connect to. Must be a valid IPv4 or IPv6 address.</param>
    /// <param name="username">The username to use for authentication with the server. Cannot be null or empty.</param>
    /// <param name="password">The password to use for authentication with the server. Cannot be null or empty.</param>
    /// <returns>A tuple containing a boolean value indicating whether the connection was successful, and a message describing
    /// the result. The boolean is <see langword="true"/> if the connection is valid; otherwise, <see
    /// langword="false"/>.</returns>
    public async Task<(bool Valid, string Message)> TestConnection(string ipAddress, string username, string password)
    {
        try
        {
            var httpClient = new ApiEVEHttpClient(ipAddress);
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
            _logger.LogError($"EveNGAdapter - TestConnection - Unknown error - {e.Message}");
            return (false, "Unknown error..");
        }
    }
}