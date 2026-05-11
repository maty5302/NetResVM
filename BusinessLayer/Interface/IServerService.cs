using BusinessLayer.DTOs;
using BusinessLayer.Models;
using BusinessLayer.Enum;

namespace BusinessLayer.Interface;

/// <summary>
/// Defines methods for managing and retrieving information about server instances, including querying, credential
/// access, and status checks.
/// </summary>
/// <remarks>Implementations of this interface provide operations for accessing server metadata, credentials, and
/// connectivity status. Methods support both synchronous and asynchronous operations for flexibility in usage
/// scenarios.</remarks>
public interface IServerService
{
    /// <summary>
    /// Retrieves a list of all available servers.
    /// </summary>
    /// <returns>A list of <see cref="ServerDTO"/> objects representing all servers. Returns <see langword="null"/> if no servers
    /// are found.</returns>
    List<ServerDTO>? GetAllServers();

    /// <summary>
    /// Retrieves the server with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the server to retrieve.</param>
    /// <returns>A <see cref="ServerDTO"/> representing the server if found; otherwise, <see langword="null"/>.</returns>
    ServerDTO? GetServerById(int id);

    /// <summary>
    /// Retrieves the server credentials associated with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the server whose credentials are to be retrieved.</param>
    /// <returns>A tuple containing the server URL, username, and password if credentials are found; otherwise, null.</returns>
    (string Url, string Username, string Password)? GetServerCredentials(int id);

    /// <summary>
    /// Gets the server platform type associated with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the server whose platform type is to be retrieved.</param>
    /// <returns>A value of type PlatformType that represents the server's platform. The value indicates the type of platform for
    /// the specified server identifier.</returns>
    PlatformType GetServerType(int id);

    /// <summary>
    /// Determines whether a server with the specified identifier exists.
    /// </summary>
    /// <param name="id">The unique identifier of the server to check for existence.</param>
    /// <returns>true if a server with the specified identifier exists; otherwise, false.</returns>
    bool ServerExists(int id);

    /// <summary>
    /// Inserts a new server record into the data store.
    /// </summary>
    /// <param name="server">The server information to insert. Cannot be null.</param>
    /// <returns>true if the server was successfully inserted; otherwise, false.</returns>
    bool InsertServer(ServerModel server);

    /// <summary>
    /// Updates the specified server with new values provided in the model.
    /// </summary>
    /// <param name="server">The server model containing updated values to apply. Cannot be null.</param>
    /// <returns>true if the server was successfully updated; otherwise, false.</returns>
    bool UpdateServer(ServerModel server);

    /// <summary>
    /// Removes the server with the specified identifier from the collection.
    /// </summary>
    /// <param name="id">The unique identifier of the server to remove.</param>
    /// <returns>true if the server was successfully removed; otherwise, false.</returns>
    bool RemoveServer(int id);

    /// <summary>
    /// Asynchronously determines whether the server at the specified IP address is online.
    /// </summary>
    /// <param name="ipAddress">The IP address of the server to check. Must be a valid IPv4 or IPv6 address.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains <see langword="true"/> if the server
    /// is online; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsServerOnlineAsync(string ipAddress);
}