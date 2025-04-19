namespace ApiCisco
{
    /// <summary>
    /// Class used for making requests to server Cisco CML API about nodes of lab
    /// </summary>
    public class ApiCiscoNode
    {
        /// <summary>
        /// Asynchronously retrieves a list of node identifiers within a specified lab on the Cisco CML server.
        /// </summary>
        /// <param name="user">The <see cref="ApiCiscoHttpClient"/> instance used to communicate with the server.</param>
        /// <param name="labId">The unique identifier of the lab to retrieve nodes from.</param>
        /// <returns>
        /// An array of <see cref="string"/> values representing the node identifiers within the lab.
        /// Returns <c>null</c> if the lab does not exist, no nodes are found, or an error occurs.
        /// </returns>
        public async Task<string[]?> GetNodes(ApiCiscoHttpClient user, string labId)
        {
            var url = user.Url + $"labs/{labId}/nodes";
            var response = await user.Client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var data = response.Content.ReadAsStringAsync().Result.Split(",");
                string[] charactersToRemove = { "\n", "\"", "[", "]", " " };
                for (int i = 0; i < data.Length; i++)
                {
                    foreach (string item in charactersToRemove)
                    {
                        data[i] = data[i].Replace(item, string.Empty);
                    }
                }
                return data;
            }
            return null;
        }

        /// <summary>
        /// Asynchronously retrieves detailed information about a specific node within a lab on the Cisco CML server.
        /// </summary>
        /// <param name="user">The <see cref="ApiCiscoHttpClient"/> instance used to send the request.</param>
        /// <param name="labId">The unique identifier of the lab that contains the node.</param>
        /// <param name="nodeId">The unique identifier of the node whose information is being retrieved.</param>
        /// <returns>
        /// A <see cref="string"/> representing the node's information (e.g., in raw JSON or another serialized format).
        /// Returns <c>null</c> if the node or lab does not exist, or if an error occurs while fetching the data.
        /// </returns>
        public async Task<string?> GetNodeInfo(ApiCiscoHttpClient user,string labId, string nodeId)
        {
            var url = user.Url + $"labs/{labId}/nodes/{nodeId}";
            var response = await user.Client.GetAsync(url.Trim());
            if (response.IsSuccessStatusCode)
            {
                var data = response.Content.ReadAsStringAsync().Result;
                return data;
            }

            return null;
        }
    }
}
