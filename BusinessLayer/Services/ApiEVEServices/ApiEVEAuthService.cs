using ApiEVE;
using SimpleLogger;
using System.Net;

namespace BusinessLayer.Services.ApiEVEServices
{
    /// <summary>
    /// Service for handling authentication with the ApiEVE server.
    /// </summary>
    public class ApiEVEAuthService
    {
        private readonly ApiEVEAuthentication authentication;
        private readonly ServerService serverService;
        private readonly ILogger logger;

        public ApiEVEAuthService()
        {
            authentication = new ApiEVEAuthentication();
            serverService = new ServerService();
            logger = FileLogger.Instance; 
        }

        /// <summary>
        /// Asynchronously validates the credentials for a given user on a specified server based on IP address.
        /// </summary>
        /// <param name="ipAddress">The IP address of the server to validate credentials against.</param>
        /// <param name="username">The username of the user attempting to authenticate.</param>
        /// <param name="password">The password of the user attempting to authenticate.</param>
        /// <returns>
        /// <c>true</c> if the credentials are valid; otherwise, <c>false</c> if the validation fails.
        /// </returns>
        public async Task<bool> ValidateCredentials(string ipAddress, string username, string password)
        {
            var response = await authentication.Authenticate(new ApiEVEHttpClient(ipAddress), username, password);
            if(response.IsSuccessStatusCode)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Asynchronously authenticates a user and creates an API client for a specified server.
        /// </summary>
        /// <param name="serverId">The unique identifier (ID) of the server to authenticate against and create the client for.</param>
        /// <returns>
        /// A tuple containing:
        /// <c>client</c> – An instance of <see cref="ApiEVEHttpClient"/> if authentication is successful; otherwise, <c>null</c>.
        /// <c>message</c> – A string message providing the result of the authentication process (success or error description).
        /// </returns>
        public async Task<(ApiEVEHttpClient? client,string message)> AuthenticateAndCreateClient(int serverId)
        {
            try
            {
                var server = serverService.GetServerByIdInternal(serverId);
                if (server == null)
                {
                    return (null, "Server not found");
                }
                var client = new ApiEVEHttpClient(server.IpAddress); // Create a new instance of ApiEVEHttpClient with the server's IP address
                var response = await authentication.Authenticate(client, server.Username, server.Password); // Authenticate the user with the server's credentials
                if (response.IsSuccessStatusCode)
                    return (client, "OK");

                logger.LogWarning($"ApiEVEAuthService - Invalid credentials - {response.StatusCode.ToString()}");
                return (null, $"Invalid credentials. Response: {response.StatusCode.ToString()}");
            }
            catch (HttpRequestException e)
            {
                logger.LogError($"ApiEVEAuthService - {e.Message}");
                return (null, "Server Unavailable");
            }
            catch (TaskCanceledException e)
            {
                logger.LogError($"ApiEVEAuthService - {e.Message}");
                return (null, "Request Timeout..");
            }
            catch(Exception e)
            {
                logger?.LogError($"ApiEVEAuthService - {e.Message}");
                return (null, "Internal server error. Contact admin.");
            }
        }

        /// <summary>
        /// Asynchronously logs out the user by invalidating the session associated with the specified API client.
        /// </summary>
        /// <param name="client">The <see cref="ApiEVEHttpClient"/> instance representing the authenticated user session to log out.</param>
        /// <remarks>
        /// This method performs the logout operation asynchronously, but does not return any result or status.
        /// </remarks>
        public async void Logout(ApiEVEHttpClient client)
        {
            var res = await authentication.Logout(client);
            if (res == null)
            {
                logger.LogError("ApiEVEAuthService - Logout failed");
            }
            else if (res.StatusCode == HttpStatusCode.OK)
            {
                return;
            }
            else
            {
                logger.LogError("ApiEVEAuthService - Logout failed");
            }
        }
    }
}
