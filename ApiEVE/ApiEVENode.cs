using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ApiEVE
{
    /// <summary>
    /// Provides methods for managing nodes in a lab in the EVE API, including retrieving node information, starting, and stopping nodes.
    /// </summary>
    public class ApiEVENode
    {
        /// <summary>
        /// Retrieves all nodes in a specific lab.
        /// </summary>
        /// <param name="client">An authenticated <see cref="ApiEVEHttpClient"/> instance.</param>
        /// <param name="labName">The name of the lab to retrieve the nodes for.</param>
        /// <returns>
        /// A tuple containing the HTTP status code and the response content as a string. 
        /// If the request fails, the string will be null.
        /// </returns>
        public async Task<(HttpStatusCode code,string? nodes)> GetAllNodes(ApiEVEHttpClient client, string labName)
        {
            var url = $"{client.Url}labs/{labName}/nodes";
            var response = await client.Client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return (response.StatusCode,await response.Content.ReadAsStringAsync());
            }
            return (response.StatusCode, null);
        }

        /// <summary>
        /// Starts a specific node in a lab.
        /// </summary>
        /// <param name="client">An authenticated <see cref="ApiEVEHttpClient"/> instance.</param>
        /// <param name="labName">The name of the lab containing the node.</param>
        /// <param name="id">The ID of the node to start.</param>
        /// <returns>
        /// The HTTP status code of the response indicating the result of the operation.
        /// </returns>
        public async Task<HttpStatusCode> StartNode(ApiEVEHttpClient client, string labName, int id)
        {
            var url = $"{client.Url}labs/{labName}/nodes/{id}/start";
            var response = await client.Client.GetAsync(url);
            return response.StatusCode;
        }

        /// <summary>
        /// Stops a specific node in a lab.
        /// </summary>
        /// <param name="client">An authenticated <see cref="ApiEVEHttpClient"/> instance.</param>
        /// <param name="labName">The name of the lab containing the node.</param>
        /// <param name="id">The ID of the node to stop.</param>
        /// <returns>
        /// The HTTP status code of the response indicating the result of the operation.
        /// </returns>
        public async Task<HttpStatusCode> StopNode(ApiEVEHttpClient client, string labName, int id)
        {
            var url = $"{client.Url}labs/{labName}/nodes/{id}/stop";
            var response = await client.Client.GetAsync(url);
            return response.StatusCode;
        }
    }
}
