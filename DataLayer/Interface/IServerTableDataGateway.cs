using System.Data;
namespace DataLayer.Interface;

/// <summary>
/// Defines methods for accessing and manipulating server records in a data store.
/// </summary>
/// <remarks>Implementations of this interface provide CRUD operations for server entities, enabling retrieval,
/// insertion, updating, and removal of server data. The interface abstracts the underlying data storage mechanism,
/// allowing for flexible implementations.</remarks>
public interface IServerTableDataGateway
{
    /// <summary>
    /// Retrieves a table containing information about all available servers.
    /// </summary>
    /// <returns>A <see cref="DataTable"/> that contains details for each server. The table will be empty if no servers are
    /// found.</returns>
    DataTable GetAllServers();

    /// <summary>
    /// Retrieves server information for the specified server identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the server to retrieve.</param>
    /// <returns>A DataTable containing the server information if found; otherwise, an empty DataTable.</returns>
    DataTable GetServerById(int id);

    /// <summary>
    /// Inserts a new server record with the specified configuration details.
    /// </summary>
    /// <param name="serverType">The type of the server to insert. This value determines the server's category or role.</param>
    /// <param name="name">The name assigned to the server. Used to identify the server within the system.</param>
    /// <param name="ipAddress">The IP address of the server. Must be a valid IPv4 or IPv6 address.</param>
    /// <param name="username">The username used to authenticate with the server.</param>
    /// <param name="password">The password associated with the specified username for server authentication.</param>
    void InsertServer(string serverType, string name, string ipAddress, string username, string password);

    /// <summary>
    /// Updates the configuration of an existing server with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier of the server to update.</param>
    /// <param name="serverType">The type of the server to update. This value determines the server's role or category.</param>
    /// <param name="name">The new name to assign to the server. Cannot be null or empty.</param>
    /// <param name="ipAddress">The IP address to assign to the server. Must be a valid IPv4 or IPv6 address.</param>
    /// <param name="username">The username used for server authentication. Cannot be null or empty.</param>
    /// <param name="password">The password used for server authentication. Cannot be null or empty.</param>
    void UpdateServer(int id, string serverType, string name, string ipAddress, string username, string password);

    /// <summary>
    /// Removes the server with the specified identifier from the collection.
    /// </summary>
    /// <param name="id">The unique identifier of the server to remove.</param>
    void RemoveServer(int id);
}