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

		//needs to be removed
		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}