using ApiCisco;
using SimpleLogger;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Services.ApiCiscoServices
{
    public class ApiCiscoAuthService
    {
        private readonly ServerService _serverService;
        private readonly Authentication _authentication;
        private readonly ILogger logger = FileLogger.Instance;

        public ApiCiscoAuthService()
        {
            _serverService = new ServerService();
            _authentication = new Authentication();
        }

        public async Task<(UserHttpClient? conn,string message)> AuthenticateAndCreateClient(int serverId)
        {
            var server = _serverService.GetServerByIdInternal(serverId);
            if (server == null)
            {
                return (null,"Server not found.");
            }
            var client = new UserHttpClient(server.IpAddress);

            var response = await _authentication.Authenticate(client, server.Username, server.Password);
            if (response == null)
            {
                logger.LogError("Couldn't fetch response from cisco API.");
                return (null, "");
            }
            else if(response.StatusCode== HttpStatusCode.ServiceUnavailable)
            {
                logger.LogError("Cisco API- Service Unavailable");
                return (null, "Service Unavailable");
            }
            else if(response.StatusCode == HttpStatusCode.Unauthorized)
            {
                logger.LogWarning("Unauthorized access. Probably bad password while trying to authenticate to cisco cml server.");
                return (null, "Unauthorized");
            }
            else if (response.StatusCode == HttpStatusCode.RequestTimeout)
            {
                logger.LogError("Cisco API - Request Timeout");
                return (null, "Request Timeout");
            }
            else if (response.StatusCode == HttpStatusCode.OK)
            {
                return (client, "Authenticated");
            }
            else
            {
                logger.LogError("Cisco API - Unknown error");
                return (null, "Unknown error");
            }
        }

        public async void Logout(UserHttpClient client)
        {
            var res = await _authentication.Logout(client);
            if (res == null)
            {
                logger.LogError("Logout failed");
            }
            else if (res.StatusCode == HttpStatusCode.OK)
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
