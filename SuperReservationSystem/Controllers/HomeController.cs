using System.Diagnostics;
using System.Net.NetworkInformation;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;
using SuperReservationSystem.Models;

namespace NetResVM.Controllers
{
    /// <summary>
    /// Controller for managing the home page and server management.
    /// </summary>
    public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
        private readonly ServerService _serverService;

        /// <summary>
        /// Constructor for the HomeController class.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="serverService"></param>
        public HomeController(ILogger<HomeController> logger, ServerService serverService)
		{
			_logger = logger;
            _serverService = serverService;
        }

        /// <summary>
        /// Handles requests to the main server overview page. Redirects unauthenticated users to the login page,
        /// displays an error message if the connection was lost, and shows a list of available servers with their
        /// online status.
        /// </summary>
        /// <remarks>If the query string contains an 'error' parameter with the value 'connection_lost',
        /// an error message is displayed to the user. The list of servers is retrieved and their online status is
        /// determined before rendering the view.</remarks>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="IActionResult"/>
        /// that renders the server overview view, or redirects to the login page or the same page with an error
        /// message.</returns>
        public async Task<IActionResult> Index()
		{			
			if (User.Identity != null && !User.Identity.IsAuthenticated)
				return RedirectToAction("Index","Login");

            if (Request.Query["error"] == "connection_lost")
            {
                TempData["ErrorMessage"] = "Connection lost! The server went offline while you were working.";
                return RedirectToAction("Index");
            }

            var servers = _serverService.GetAllServers();

            if(servers== null || servers.Count == 0)
            {
                ViewBag.Servers = new List<BusinessLayer.DTOs.ServerDTO>();
                return View();
            }

            var pingTasks = servers.Select(async (server) =>
            {
                server.IsOnline = await PingServerAsync(server.IpAddress);
                return server;
            });

            await Task.WhenAll(pingTasks);

            ViewBag.Servers = servers;
            return View();
		}

        /// <summary>
        /// Asynchronously determines whether the specified server is reachable by sending an ICMP echo request (ping).
        /// </summary>
        /// <remarks>If the server is unreachable, the DNS lookup fails, or a network error occurs, the
        /// method returns <see langword="false"/>. The ping operation uses a 1000 millisecond timeout to avoid
        /// indefinite waiting.</remarks>
        /// <param name="ipAddressOrHostname">The IP address or DNS hostname of the server to ping. Cannot be null, empty, or consist only of white-space
        /// characters.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the server
        /// responds to the ping request within the timeout period; otherwise, <see langword="false"/>.</returns>
        private async Task<bool> PingServerAsync(string ipAddressOrHostname)
        {
            if (string.IsNullOrWhiteSpace(ipAddressOrHostname)) return false;

            try
            {
                using var pinger = new Ping();
                // 1000ms timeout so the page doesn't hang forever on dead servers
                PingReply reply = await pinger.SendPingAsync(ipAddressOrHostname, 1000);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                // If DNS fails or network is unreachable, it throws an exception. Treat as offline.
                return false;
            }
        }

        //needs to be removed
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}