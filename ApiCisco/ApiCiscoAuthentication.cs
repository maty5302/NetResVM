using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ApiCisco
{
    /// <summary>
    /// This class is used for authentication with server CML API
    /// </summary>
    public class ApiCiscoAuthentication
    {
        /// <summary>
        /// Asynchronously authenticates a user using the specified Cisco API client and credentials.
        /// </summary>
        /// <param name="client">The <see cref="ApiCiscoHttpClient"/> instance used to send the authentication request.</param>
        /// <param name="username">The username of the user to authenticate.</param>
        /// <param name="password">The password of the user to authenticate.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> representing the response from the server,
        /// including status code and any returned data related to the authentication attempt.
        /// </returns>
        public async Task<HttpResponseMessage> Authenticate(ApiCiscoHttpClient client, string username, string password)
        {
            var url = client.Url + "authenticate";
            var authData = new
            {
                username = username,
                password = password
            };
            string jsondata = JsonSerializer.Serialize(authData);
            var content = new StringContent(jsondata, Encoding.UTF8, "application/json");
            var response = await client.Client.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                client.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                    response.Content.ReadAsStringAsync().Result.Replace("\"", ""));
                return response;
            }
            return response;
        }

        /// <summary>
        /// Asynchronously logs out the currently authenticated user using the specified Cisco API client.
        /// </summary>
        /// <param name="client">The <see cref="ApiCiscoHttpClient"/> instance representing the current user session.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> containing the result of the logout request,
        /// including status code and any response data from the server.
        /// </returns>
        public async Task<HttpResponseMessage> Logout(ApiCiscoHttpClient client)
        {
            var url = client.Url + "logout";
            var response = await client.Client.DeleteAsync(url);
            if (response.IsSuccessStatusCode)
            {
                client.Client.DefaultRequestHeaders.Authorization = null;
                return response;
            }
            return response;
        }

    }

}
