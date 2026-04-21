using BusinessLayer.DTOs;
using BusinessLayer.Enum;
using Microsoft.AspNetCore.Http;

namespace BusinessLayer.Interface;

/// <summary>
/// Defines the contract for a virtualization platform adapter, providing methods to manage, authenticate, and interact
/// with virtual labs and their resources.
/// </summary>
/// <remarks>Implementations of this interface enable integration with different virtualization platforms by
/// abstracting platform-specific operations such as authentication, lab import/export, and node management. Methods
/// typically return result objects containing both operation status and descriptive messages to facilitate error
/// handling and diagnostics.</remarks>
public interface IVirtualizationAdapter
{
    /// <summary>
    /// Gets the name of the platform on which the application is running.
    /// </summary>
    PlatformType PlatformName { get; }
    
    /// <summary>
    /// Attempts to establish a connection to the specified host using the provided credentials.
    /// </summary>
    /// <param name="ipAddress">The IP address of the host to connect to. Must be a valid IPv4 or IPv6 address.</param>
    /// <param name="username">The username to use for authentication when connecting to the host. Cannot be null or empty.</param>
    /// <param name="password">The password associated with the specified username. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains a boolean indicating whether the
    /// connection was successful, and a message describing the outcome.</returns>
    Task<(bool Valid, string Message)>TestConnection(string ipAddress, string username, string password);
    
    /// <summary>
    /// Asynchronously authenticates the user with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user to authenticate.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains a tuple where Valid is true if
    /// authentication succeeds; otherwise, false. Message provides additional information about the authentication
    /// result.</returns>
    Task<(bool Valid, string Message)> AuthenticateAsync(int id);

    /// <summary>
    /// Asynchronously retrieves the list of labs associated with the specified server.
    /// </summary>
    /// <param name="serverId">The unique identifier of the server for which to retrieve labs.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains a tuple with a list of labs as <see
    /// cref="LabDTO"/> objects, or <see langword="null"/> if no labs are found, and a message describing the result.</returns>
    Task<(List<LabDTO>? Labs, string Message)> GetLabsAsync(int serverId);
    
    /// <summary>
    /// Asynchronously retrieves detailed information about a specific lab.
    /// </summary>
    /// <param name="serverId">The unique identifier of the server hosting the lab.</param>
    /// <param name="labId">The unique identifier of the lab to retrieve information for.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains a tuple with the lab information as a <see cref="LabDTO"/> object, or <see langword="null"/> if the lab is not found, and a message describing the result.</returns>
    Task<(LabDTO? Lab, string Message)> GetLabInfoAsync(int serverId, string labId);
    
    /// <summary>
    /// Imports laboratory data from the specified file content to the server identified by the given server ID.
    /// </summary>
    /// <param name="serverId">The unique identifier of the server to which the laboratory data will be imported.</param>
    /// <param name="fileContent">The binary content of the file containing laboratory data to import. Cannot be null.</param>
    /// <param name="filename">The name of the file being imported, or null if not specified. Used for logging or display purposes.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the import
    /// succeeds; otherwise, <see langword="false"/>.</returns>
    Task<bool> ImportLab(int serverId, byte[] fileContent, string? filename = null);
    
    /// <summary>
    /// Imports laboratory data from the specified file to the server identified by the given server ID.
    /// </summary>
    /// <param name="serverId">The unique identifier of the server to which the laboratory data will be imported.</param>
    /// <param name="file">The file containing laboratory data to import. Must not be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the import
    /// succeeds; otherwise, <see langword="false"/>.</returns>
    Task<bool> ImportLab(int serverId, IFormFile file);
    
    /// <summary>
    /// Downloads the specified laboratory file from the server and returns its content, content type, file name, and an
    /// informational message.
    /// </summary>
    /// <param name="serverId">The unique identifier of the server from which to download the laboratory file.</param>
    /// <param name="labId">The identifier of the laboratory to download. Can be null if the laboratory is specified by the <paramref
    /// name="lab"/> parameter.</param>
    /// <param name="lab">An optional laboratory data transfer object representing the laboratory to download. If null, the method uses
    /// <paramref name="labId"/> to identify the laboratory.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains a tuple with the file content as a byte
    /// array (or null if not found), the MIME content type, the file name, and an informational message.</returns>
    Task<(byte[]? FileContent, string ContentType, string FileName, string Message)> DownloadLab(int serverId, string? labId, LabDTO? lab = null);
    
    /// <summary>
    /// Deletes the specified lab from the given server asynchronously.
    /// </summary>
    /// <param name="serverId">The unique identifier of the server from which to delete the lab.</param>
    /// <param name="labId">The unique identifier of the lab to delete.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains a boolean value indicating whether the
    /// deletion was successful, and a message providing additional information about the operation.</returns>
    Task<(bool value, string message)> DeleteLab(int serverId, string labId);
    
    /// <summary>
    /// Asynchronously retrieves the current state of the specified laboratory on the given server.
    /// </summary>
    /// <param name="serverId">The unique identifier of the server hosting the laboratory.</param>
    /// <param name="labId">The unique identifier of the laboratory whose state is to be retrieved. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a string representing the current
    /// state of the laboratory, or null if the laboratory is not found.</returns>
    Task<string?> StateOfLab(int serverId, string labId);

    /// <summary>
    /// Asynchronously starts the specified lab on the given server.
    /// </summary>
    /// <param name="serverId">The unique identifier of the server on which to start the lab.</param>
    /// <param name="labId">The unique identifier of the lab to start.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains a boolean value indicating whether the
    /// lab was started successfully, and a message providing additional information about the operation.</returns>
    Task<(bool value, string message)> StartLabAsync(int serverId, string labId);
    
    /// <summary>
    /// Asynchronously stops the specified lab instance on the given server.
    /// </summary>
    /// <param name="serverId">The unique identifier of the server hosting the lab to stop.</param>
    /// <param name="labId">The unique identifier of the lab instance to stop.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains a boolean value indicating whether the
    /// operation succeeded, and a message providing additional information about the result.</returns>
    Task<(bool value, string message)> StopLabAsync(int serverId, string labId);

    /// <summary>
    /// Asynchronously retrieves all nodes associated with the specified server and lab.
    /// </summary>
    /// <param name="serverId">The unique identifier of the server from which to retrieve nodes.</param>
    /// <param name="labId">The unique identifier of the lab whose nodes are to be retrieved. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains a tuple with a list of node data transfer
    /// objects if found, and a message describing the outcome. The list is null if no nodes are found.</returns>
    Task<(List<NodeDTO>? Nodes, string Message)> GetAllNodes(int serverId, string labId);

}