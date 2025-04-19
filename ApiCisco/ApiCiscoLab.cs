using System.Net;
using System.Text;

namespace ApiCisco
{
    /// <summary>
    /// Class used for making requests to server Cisco CML API about labs 
    /// </summary>
    public class ApiCiscoLab
    {
        /// <summary>
        /// Asynchronously retrieves the list of lab identifiers available to the specified Cisco API client.
        /// </summary>
        /// <param name="user">The <see cref="ApiCiscoHttpClient"/> instance used to perform the request.</param>
        /// <returns>
        /// An array of lab IDs as <see cref="string"/> values if the request is successful; otherwise, <c>null</c> if an error occurs.
        /// </returns>
        public async Task<string[]?> GetLabs(ApiCiscoHttpClient user)
        {
            var url = user.Url + "labs";
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
        /// Asynchronously retrieves detailed information about a specific lab from the Cisco CML server.
        /// Optionally downloads the lab content.
        /// </summary>
        /// <param name="client">The <see cref="ApiCiscoHttpClient"/> used to send the request.</param>
        /// <param name="labId">The unique identifier of the lab to retrieve.</param>
        /// <param name="download">
        /// If set to <c>true</c>, the method will also attempt to download the lab's content.
        /// Otherwise, it retrieves only the metadata or summary.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> containing the lab information (e.g., raw JSON or other serialized format),
        /// or <c>null</c> if the request fails or the lab is not found.
        /// </returns>
        public async Task<string?> GetLabInfo2(ApiCiscoHttpClient client, string labId, bool download = false)
        {

            var url = $"{client.Url}labs/{labId.Trim()}";
            if (download)
                url = url + "/download";
            var response = await client.Client.GetAsync(url.Trim());
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
                return null;
        }

        /// <summary>
        /// Asynchronously starts or stops a lab on the Cisco CML server.
        /// </summary>
        /// <param name="client">The <see cref="ApiCiscoHttpClient"/> used to send the request.</param>
        /// <param name="labId">The unique identifier of the lab to start or stop.</param>
        /// <param name="start">
        /// If set to <c>true</c>, the lab will be started; if <c>false</c>, the lab will be stopped.
        /// </param>
        /// <returns>
        /// A tuple containing:
        /// <c>result</c> – <c>true</c> if the operation was successful; otherwise, <c>false</c>.
        /// <c>message</c> – An <see cref="HttpStatusCode"/> indicating the response status from the server.
        /// </returns>
        public async Task<(bool result, HttpStatusCode message)> StartStopLab(ApiCiscoHttpClient client, string labId, bool start = true)
        {
            var url = $"{client.Url}labs/{labId.Trim()}";
            if (start)
                url += "/start";
            else
                url += "/stop";

            var response = await client.Client.PutAsync(url.Trim(), null);
            if (response.StatusCode == HttpStatusCode.NoContent)
                return (true, response.StatusCode);
            else
                return (false, response.StatusCode);
        }

        /// <summary>
        /// Asynchronously imports a lab into the Cisco CML server from the specified file.
        /// </summary>
        /// <param name="user">The <see cref="ApiCiscoHttpClient"/> instance used to perform the import request.</param>
        /// <param name="file"> Content of the file which will be formated as a json in this method</param>
        /// <returns>
        /// <c>true</c> if the lab import was successful; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> ImportLab(ApiCiscoHttpClient user, string file)
        {
            var url = user.Url + "import";
            try
            {
                var content = new StringContent(file, Encoding.UTF8, "application/json");
                var response = await user.Client.PostAsync(url.Trim(), content);
                if (response.StatusCode == HttpStatusCode.OK)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to import the lab.", ex);
            }
        }

        /// <summary>
        /// Asynchronously retrieves the current state of a specified lab on the Cisco CML server.
        /// </summary>
        /// <param name="user">The <see cref="ApiCiscoHttpClient"/> instance used to communicate with the server.</param>
        /// <param name="labId">The unique identifier of the lab whose state is being queried.</param>
        /// <returns>
        /// A <see cref="string"/> representing the current state of the lab (e.g., "STARTED", "STOPPED", "BOOTED").
        /// Returns <c>null</c> if the lab does not exist or the request fails.
        /// </returns>
        public async Task<string?> StateOfLab(ApiCiscoHttpClient user, string labId)
        {
            var url = $"{user.Url}labs/{labId.Trim()}/state";
            var response = await user.Client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }

        /// <summary>
        /// Asynchronously deletes a specific lab from the Cisco CML server.
        /// </summary>
        /// <param name="user">The <see cref="ApiCiscoHttpClient"/> instance used to perform the request.</param>
        /// <param name="labId">The unique identifier of the lab to be deleted.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> representing the server's response to the deletion request,
        /// including the status code and any additional response content.
        /// </returns>
        public async Task<HttpResponseMessage> DeleteLab(ApiCiscoHttpClient user, string labId)
        {
            var url = $"{user.Url}labs/{labId.Trim()}";
            var response = await user.Client.DeleteAsync(url);
            return response;
        }
    }
}
