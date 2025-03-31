using ApiEVE;
using BusinessLayer.Models;
using SimpleLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BusinessLayer.Services.ApiEVEServices
{
    public class ApiEVENodeService
    {
        private readonly ApiEVEAuthService apiEVEAuthService;
        private readonly ApiEVENode apiEVENode;
        private static ILogger logger = FileLogger.Instance;

        public ApiEVENodeService()
        {
            this.apiEVEAuthService = new ApiEVEAuthService();
            this.apiEVENode = new ApiEVENode();
        }

        public async Task<List<EVENodeModel>?> GetAllNodes(int serverId, string labName)
        {
            var client = await apiEVEAuthService.AuthenticateAndCreateClient(serverId);
            if (client.client == null)
            {
                return null;
            }
            var nodes = await apiEVENode.GetAllNodes(client.client,labName);
            if (nodes.nodes == null)
            {
                logger.LogError($"ApiEVENodeService - GetAllNodes - {nodes.code}");
                return null;
            }
            var jsonDoc = JsonDocument.Parse(nodes.nodes);
            var labsElement = jsonDoc.RootElement.GetProperty("data");
            List<EVENodeModel> allNodes = new List<EVENodeModel>();
            foreach (var lab in labsElement.EnumerateObject())
            {
                var res = JsonSerializer.Deserialize<EVENodeModel>(lab.Value);
                if(res == null)
                    continue;
                allNodes.Add(res);
            }
            return allNodes;
        }

        public async Task<bool> StartNode(int serverId, string labName, int nodeId)
        {
            var client = await apiEVEAuthService.AuthenticateAndCreateClient(serverId);
            if (client.client == null)
            {
                return false ;
            }
            var res = await apiEVENode.StartNode(client.client, labName, nodeId);
            if(res != System.Net.HttpStatusCode.OK)
            {
                logger.LogError($"Failed to start node in {labName}. Reason: {res}");
                return false;
            }
            logger.Log($"Node with ID {nodeId} in {labName} started.");
            return true;
        }

        public async Task<bool> StopNode(int serverId, string labName, int nodeId)
        {
            var client = await apiEVEAuthService.AuthenticateAndCreateClient(serverId);
            if (client.client == null)
            {
                return false;
            }
            var res = await apiEVENode.StopNode(client.client, labName, nodeId);
            if (res != System.Net.HttpStatusCode.OK)
            {
                logger.LogError($"Failed to stop node in {labName}. Reason: {res}");
                return false;
            }
            logger.Log($"Node with ID {nodeId} in {labName} stopped.");
            return true;
        }

        public async Task<bool> StartAllNodes(int serverId, string labName)
        {
            var nodes = await GetAllNodes(serverId, labName);
            if (nodes == null || nodes.Count==0)
            {
                logger.LogWarning($"Lab don't have any nodes aborting..");
                return false;
            }
            foreach (var node in nodes)
            {
                await StartNode(serverId, labName,node.Id);
            }
            return true;
        }

        public async Task<bool> StopAllNodes (int serverId, string labName)
        {
            var nodes = await GetAllNodes(serverId, labName);
            if (nodes == null || nodes.Count == 0)
            {
                logger.LogWarning($"Lab don't have any nodes aborting..");
                return false;
            }
            foreach (var node in nodes)
            {
                await StopNode(serverId, labName, node.Id);
            }
            return true;
        }
    }
}
