using System.Net;
using System.Text;
using System.Text.Json;

namespace ApiEVE
{
    /// <summary>
    // /// Provides authentication services for the EVE API, including login and logout operations.
    // /// </summary>
    public class ApiEVEAuthentication
    {
        /// <summary>
        /// Authenticates the user by sending login credentials to the API and stores the session cookie.
        /// </summary>
        /// <param name="client">The client instance containing the URL and HTTP client.</param>
        /// <param name="username">The username for authentication.</param>
        /// <param name="password">The password for authentication.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation, with an <see cref="HttpResponseMessage"/> result.</returns>
        /// <exception cref="HttpRequestException">Thrown if the request fails or the response status code indicates failure.</exception>
        public async Task<HttpResponseMessage> Authenticate(ApiEVEHttpClient client,string username, string password)
        {
            var loginUrl = $"{client.Url}auth/login";
            var authData = new
            {
                username = username,
                password = password,
                html5 = "-1"
            };

            var content = new StringContent(JsonSerializer.Serialize(authData), Encoding.UTF8,"application/json");
            var response = await client.Client.PostAsync(loginUrl, content);
            response.EnsureSuccessStatusCode();
            var authCookies = response.Headers.GetValues("Set-Cookie").First().Split(';');
            var session = authCookies[0].Split('=');
            Cookie auth = new Cookie(session[0], session[1]);
            client.cookieContainer.Add(new Uri(client.Url), auth);
            return response;
        }

        /// <summary>
        /// Logs the user out by sending a logout request to the API.
        /// </summary>
        /// <param name="client">The client instance containing the URL and HTTP client.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation, with an <see cref="HttpResponseMessage"/> result.</returns>
        /// <exception cref="HttpRequestException">Thrown if the request fails or the response status code indicates failure.</exception>
        public async Task<HttpResponseMessage> Logout(ApiEVEHttpClient client)
        {
            var logoutUrl = $"{client.Url}auth/logout";
            var response = await client.Client.GetAsync(logoutUrl);
            return response;
        }
    }
}
