using Microsoft.AspNetCore.Mvc;
using SuperReservationSystem.Models;
using System.Diagnostics;
using ApiCisco;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using BusinessLayer;
using BusinessLayer.Services;
using BusinessLayer.Models;

namespace SuperReservationSystem.Controllers
{
    public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private ServerService serverService = new ServerService();

        public HomeController(ILogger<HomeController> logger)
		{
			_logger = logger;
		}

		public IActionResult Index()
		{			
			if (!User.Identity.IsAuthenticated)
				return RedirectToAction("Index","Login");			
            ViewBag.Servers = serverService.GetAllServers();
            return View();
		}

		public IActionResult Privacy()
		{
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Index","Login");
            return View();
		}

        //[Authorize(Roles = "Admin")]
        public IActionResult AddServer()
		{
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Index","Login");
			if (!User.IsInRole("Admin"))
				return RedirectToAction("Index");
            //only admin
            return View();
		}

		public IActionResult RemoveServer(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Index");
            //only admin
            serverService.RemoveServer(id);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> TestConnection(ServerModel server)
		{
			if(!ModelState.IsValid)
			{
				return View("Error");
			}
            if (server.IpAddress.Contains("http://"))
                server.IpAddress = server.IpAddress.Replace("http://", "https://");
            else if (!server.IpAddress.StartsWith("http"))
                server.IpAddress = "https://" + server.IpAddress;
            var client = new UserHttpClient(server.IpAddress);
			var res = await Authentication.Authenticate(client, server.Username, server.Password);
			if (res != null && res.IsSuccessStatusCode)
			{
				TempData["SuccessMessage"] = "Connection successful";				
				ViewBag.Tested = true;
				return View("AddServer", server);
			}
			else
			{
				if(res!=null)
					TempData["ErrorMessage"] = "Connection failed. " + res.Content.ReadAsStringAsync().Result;
				return View("AddServer", server);
			}

		}

		[HttpPost]
		public async Task<IActionResult> AddTestedConnection(ServerModel server)
		{

			if(!ModelState.IsValid)
			{
                return RedirectToAction("AddServer", "Home");
            }

			if (server.ServerType == "CML")
			{
                if (server.IpAddress.Contains("http://"))
                    server.IpAddress = server.IpAddress.Replace("http://", "https://");
                else if (!server.IpAddress.StartsWith("http"))
                    server.IpAddress = "https://" + server.IpAddress;
                var client = new UserHttpClient(server.IpAddress);				
                var res = await Authentication.Authenticate(client, server.Username, server.Password);
                if (res != null && res.IsSuccessStatusCode)
				{
					var ok = serverService.InsertServer(server);
                    ViewBag.Servers = serverService.GetAllServers();
                    TempData["SuccessMessage"] = "Server added successfully";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    if (res != null)
                        TempData["ErrorMessage"] = "Connection failed. " + res.Content.ReadAsStringAsync().Result;
                    return View("AddServer", server);
                }

            }
			else
				return RedirectToAction("Index", "Home");
		}

		//needs to be removed
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}