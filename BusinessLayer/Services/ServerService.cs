using DataLayer;
using BusinessLayer.Models;
using BusinessLayer.MapperDT;
using SimpleLogger;
using BusinessLayer.DTOs;

namespace BusinessLayer.Services
{
    /// <summary>
    /// Service class for managing server operations.
    /// </summary>
    public class ServerService
    {
        private readonly ServerTableDataGateway _gateway;
        private static ILogger _logger = FileLogger.Instance;

        public ServerService()
        {
            _gateway = new ServerTableDataGateway();
        }

        /// <summary>
        /// Retrieves a list of all servers available in the system.
        /// </summary>
        /// <returns>
        /// A list of <see cref="ServerDTO"/> objects representing the servers,
        /// or <c>null</c> if no servers are found or an error occurs.
        /// </returns>
        public List<ServerDTO>? GetAllServers()
        {
            try
            {
                var servers = new List<ServerDTO>();
                var table = _gateway.GetAllServers();
                foreach (System.Data.DataRow row in table.Rows)
                {
                    servers.Add(ServerMapper.MapToDTO(row));
                }
                return servers;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Retrieves a server by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier (ID) of the server.</param>
        /// <returns>
        /// A <see cref="ServerDTO"/> object if a server with the specified ID exists; otherwise, <c>null</c>.
        /// </returns>
        public ServerDTO? GetServerById(int id)
        {
            try
            {
                var table = _gateway.GetServerById(id);
                return ServerMapper.MapToDTO(table.Rows[0]);
            }
            catch
            {
                _logger.LogError($"Server with id {id} not found.");
                return null;
            }
        }

        /// <summary>
        /// Retrieves a server entity from the internal data source by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier (ID) of the server.</param>
        /// <returns>
        /// A <see cref="ServerModel"/> object if the server exists; otherwise, <c>null</c>.
        /// </returns>
        /// <remarks> This method is intended for internal use within the business logic layer. </remarks>
        internal ServerModel? GetServerByIdInternal(int id)
        {
            try
            {
                var table = _gateway.GetServerById(id);
                return ServerMapper.Map(table.Rows[0]);
            }
            catch
            {
                _logger.LogError($"Server with id {id} not found.");
                return null;
            }
        }

        /// <summary>
        /// Retrieves the type of a server by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier (ID) of the server.</param>
        /// <returns>
        /// The server type as a string if the server exists; otherwise, an empty string.
        /// </returns>
        public string GetServerType(int id)
        {
            try
            {
                var server=GetServerByIdInternal(id);
                if (server == null)
                    return "";
                return server.ServerType;

            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Checks whether a server with the specified ID exists in the system.
        /// </summary>
        /// <param name="id">The unique identifier (ID) of the server.</param>
        /// <returns>
        /// <c>true</c> if the server exists; otherwise, <c>false</c>.
        /// </returns>
        public bool ServerExists(int id)
        {
            try
            {
                var table = _gateway.GetServerById(id);
                return table.Rows.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Inserts a new server into the system.
        /// </summary>
        /// <param name="server">The <see cref="ServerModel"/> object containing the server details to insert.</param>
        /// <returns>
        /// <c>true</c> if the server was successfully inserted; otherwise, <c>false</c>.
        /// </returns>
        public bool InsertServer(ServerModel server)
        {
            var servers = GetAllServers();
            if (servers != null)
            {
                if(servers.Any(servers => servers.Name == server.Name))
                {
                    _logger.LogWarning($"Server with name {server.Name} already exists.");
                    return false;
                }
            }
            else
            {
                _logger.LogError("Couldn't fetch servers from database.");
                return false;
            }
            try
            {
                if (server.ServerType == "EVE" && !server.IpAddress.StartsWith("http")) 
                    server.IpAddress = "http://" + server.IpAddress;
                else if (server.ServerType == "CML" && !server.IpAddress.StartsWith("http"))
                    server.IpAddress = "https://" + server.IpAddress;
                string ip = new Uri(server.IpAddress).Host; //To get the IP address from the URL
                _gateway.InsertServer(server.ServerType, server.Name, ip, server.Username, server.Password);
                _logger.Log($"Server with name {server.Name} has been inserted.");
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError($"Server with name {server.Name} couldn't be inserted. {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates the details of an existing server in the system.
        /// </summary>
        /// <param name="server">The <see cref="ServerModel"/> object containing the updated server details.</param>
        /// <returns>
        /// <c>true</c> if the server was successfully updated; otherwise, <c>false</c>.
        /// </returns>
        public bool UpdateServer(ServerModel server)
        {
            var serverToUpdate = GetServerByIdInternal(server.Id);
            if (serverToUpdate == null)
            {
                return false;
            }
            if (server.Password == String.Empty)
            {
                server.Password = serverToUpdate.Password;
            }
            try
            {
                _gateway.UpdateServer(server.Id, server.ServerType, server.Name, server.IpAddress, server.Username, server.Password);
                _logger.Log($"Server with id {server.Id} has been updated.");
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError($"Server with id {server.Id} couldn't be updated. {e.Message}" );
                return false;
            }
        }

        /// <summary>
        /// Removes a server from the system by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier (ID) of the server to remove.</param>
        /// <returns>
        /// <c>true</c> if the server was successfully removed; otherwise, <c>false</c>.
        /// </returns>
        public bool RemoveServer(int id)
        {
            var server = GetServerById(id);
            if (server == null)
            {
                return false;
            }
            try
            {
                _gateway.RemoveServer(id);
                _logger.Log($"Server with id {id} has been removed.");
                return true;
            }
            catch(Exception e)
            {
                _logger.LogError($"Server with id {id} couldn't be removed. {e.Message}");
                return false;
            }
        }
    }
}
