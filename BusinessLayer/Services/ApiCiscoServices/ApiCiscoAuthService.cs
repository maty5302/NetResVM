using ApiCisco;
using SimpleLogger;
using System.Net;

namespace BusinessLayer.Services.ApiCiscoServices
{
    /// <summary>
    /// Provides authentication and session management for Cisco CML servers.
    /// </summary>
    /// <remarks>
    /// This service uses <see cref="ApiCiscoHttpClient"/> to connect to Cisco CML servers
    /// and handles login, logout, and credential validation.
    /// </remarks>
    public class ApiCiscoAuthService
    {
        private readonly ServerService _serverService;
        private readonly ApiCiscoAuthentication _authentication;
        private readonly ILogger logger = FileLogger.Instance;

        public ApiCiscoAuthService()
        {
            _serverService = new ServerService();
            _authentication = new ApiCiscoAuthentication();
        }

        /// <summary>
        /// Asynchronously validates user credentials against a Cisco CML server at the specified IP address.
        /// </summary>
        /// <param name="ipAddress">The IP address of the Cisco CML server.</param>
        /// <param name="username">The username to authenticate with.</param>
        /// <param name="password">The password for the given username.</param>
        /// <returns>
        /// A tuple containing a boolean indicating success and a message describing the result (e.g., error reason).
        /// </returns>
        /// <remarks>
        /// Returns specific messages for common HTTP errors (e.g., 401 Unauthorized, 503 Service Unavailable).
        /// Logs all significant events using the application logger.
        /// </remarks>
        public async Task<(bool Valid, string Message)> ValidateCredentials(string ipAddress,string username, string password)
        {
            try
            {
                var response = await _authentication.Authenticate(new ApiCiscoHttpClient(ipAddress), username, password);
                
                    
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    logger.LogError("ApiCiscoAuthService - Service Unavailable");
                    return (false, "Service Unavailable");
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                {
                    logger.LogWarning("Unauthorized access. Probably bad password while trying to authenticate to cisco cml server.");
                    return (false, "Invalid credentials");
                }
                else if (response.StatusCode == HttpStatusCode.RequestTimeout)
                {
                    logger.LogError("ApiCiscoAuthService - Request Timeout");
                    return (false, "Request Timeout");
                }
                else if (response.StatusCode == HttpStatusCode.OK)
                    return (true, "");
                else
                {
                    logger.LogError("ApiCiscoAuthService - Unknown error");
                    return (false, "Unknown error..");
                }
            }
            catch (Exception e)
            {
                logger.LogError("ApiCiscoAuthService - Unknown error");
                return (false, "Unknown error..");
            }
        }

        /// <summary>
        /// Asynchronously authenticates to a Cisco CML server using stored server credentials
        /// and returns a configured <see cref="ApiCiscoHttpClient"/> instance upon success.
        /// </summary>
        /// <param name="serverId">The ID of the server in the internal database.</param>
        /// <returns>
        /// A tuple containing a connected <see cref="ApiCiscoHttpClient"/> and a result message.
        /// If authentication fails, the client will be <c>null</c>.
        /// </returns>
        /// <remarks>
        /// Useful for initiating secure sessions to Cisco CML servers without manual credential input.
        /// </remarks>
        public async Task<(ApiCiscoHttpClient? conn, string message)> AuthenticateAndCreateClient(int serverId)
        {
            try
            {
                var server = _serverService.GetServerByIdInternal(serverId);
                if (server == null)
                {
                    return (null, "Server not found.");
                }

                var client = new ApiCiscoHttpClient(server.IpAddress);

                var response = await _authentication.Authenticate(client, server.Username, server.Password);

                if (response.IsSuccessStatusCode)
                    return (client, "OK");

                logger.LogWarning($"ApiCiscoAuthService - Invalid credentials. Response: {response.StatusCode.ToString()}");
                return (null, $"Invalid credentials. Response: {response.StatusCode.ToString()}");
            }
            catch (HttpRequestException e)
            {
                logger.LogError("ApiCiscoAuthService - Request Timeout");
                return (null, "Request Timeout");
            }
            catch (TaskCanceledException e)
            {
                logger.LogError("ApiCiscoAuthService - Service Unavailable");
                return (null, "Service Unavailable");
            }
            catch (Exception e)
            {
                logger.LogError("ApiCiscoAuthService - Unknown error");
                return (null, "Unknown error");
            }
        }

        /// <summary>
        /// Asynchronously logs out from the Cisco CML server using the provided client instance.
        /// </summary>
        /// <param name="client">The authenticated <see cref="ApiCiscoHttpClient"/> instance.</param>
        /// <remarks>
        /// Logs success or failure using the application logger.
        /// </remarks>
        public async void Logout(ApiCiscoHttpClient client)
        {
            var res = await _authentication.Logout(client); 
            if (res.StatusCode == HttpStatusCode.OK)
            {
                logger.Log("Logout successful");
            }
            else
            {
                logger.LogError("Logout failed");
            }
        }
    }

}
