using ApiCisco;
using ApiEVE;
using SimpleLogger;
using System.ComponentModel;
using System.Net;

namespace BusinessLayer.Services.ApiEVEServices
{
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

        public async Task<bool> ValidateCredentials(string ipAddress, string username, string password)
        {
            var response = await authentication.Authenticate(new ApiEVEHttpClient(ipAddress), username, password);
            if(response.IsSuccessStatusCode)
            {
                return true;
            }
            return false;
        }

        public async Task<(ApiEVEHttpClient? client,string message)> AuthenticateAndCreateClient(int serverId)
        {
            try
            {
                var server = serverService.GetServerByIdInternal(serverId);
                if (server == null)
                {
                    return (null, "Server not found");
                }
                var client = new ApiEVEHttpClient(server.IpAddress);
                var response = await authentication.Authenticate(client, server.Username, server.Password);
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
