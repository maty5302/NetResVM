using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ApiCisco;
using BusinessLayer.Models;
using SimpleLogger;

namespace BusinessLayer.Services.ApiCiscoServices
{
    /// <summary>
    /// Service class that handles operations related to nodes within Cisco CML labs.
    /// </summary>
    /// <remarks>
    /// Uses authentication via <see cref="ApiCiscoAuthService"/> and communication with the Cisco CML API via <see cref="ApiCiscoNode"/>.
    /// </remarks>
    public class ApiCiscoNodeService
    {
        private readonly ApiCiscoNode _apiCiscoNode;
        private readonly ApiCiscoAuthService _authService;
        private readonly ILogger _logger = FileLogger.Instance;

        public ApiCiscoNodeService()
        {
            _apiCiscoNode = new ApiCiscoNode();
            _authService = new ApiCiscoAuthService();
        }

        /// <summary>
        /// Asynchronously retrieves detailed information about all nodes within a specified lab on a Cisco CML server.
        /// </summary>
        /// <param name="serverId">The ID of the server to connect to.</param>
        /// <param name="labId">The unique identifier of the lab containing the nodes.</param>
        /// <returns>
        /// A list of <see cref="CiscoNodeModel"/> objects representing the nodes in the lab,
        /// or <c>null</c> if the operation fails or the client cannot be authenticated.
        /// </returns>
        /// <remarks>
        /// Each node is retrieved and deserialized individually using <see cref="GetNodeInfo"/>.
        /// </remarks>
        public async Task<List<CiscoNodeModel>?> GetNodes(int serverId, string labId)
        {
            var client = await _authService.AuthenticateAndCreateClient(serverId);
            if (client.conn == null)
            {
                return null;
            }

            try
            {
                var response = await _apiCiscoNode.GetNodes(client.conn, labId);
                if (response == null)
                {
                    _logger.LogError($"ApiCiscoNodeService - Couldn't retrieve nodes from a lab {labId}..");
                    return null;
                }
                List<CiscoNodeModel> nodes = new List<CiscoNodeModel>();
                foreach (var node in response)
                {
                    var deserNode = await GetNodeInfo(serverId, labId, node);
                    if(deserNode!=null)
                        nodes.Add(deserNode);
                }
                return nodes;
            }
            catch (Exception e)
            {
                _logger.LogError($"ApiCiscoNodeService - Couldn't retrieve nodes from a lab.. {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Asynchronously retrieves detailed information about a specific node in a Cisco CML lab.
        /// </summary>
        /// <param name="serverId">The ID of the server to connect to.</param>
        /// <param name="labId">The unique identifier of the lab containing the node.</param>
        /// <param name="nodeId">The unique identifier of the node to retrieve.</param>
        /// <returns>
        /// A <see cref="CiscoNodeModel"/> representing the node’s details,
        /// or <c>null</c> if retrieval or deserialization fails.
        /// </returns>
        public async Task<CiscoNodeModel?> GetNodeInfo(int serverId, string labId, string nodeId)
        {
            var client = await _authService.AuthenticateAndCreateClient(serverId);
            if (client.conn == null)
            {
                return null;
            }

            var response = await _apiCiscoNode.GetNodeInfo(client.conn, labId, nodeId);
            if (response == null)
            {
                _logger.LogWarning($"ApiCiscoNodeService - Couldn't retrieve node info {nodeId} from a lab {labId}");
                return null;
            }

            var node = JsonSerializer.Deserialize<CiscoNodeModel>(response);
            if (node == null)
                return null;
            return node;
        }
    }
}
