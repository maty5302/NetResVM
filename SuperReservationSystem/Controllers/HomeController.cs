using Microsoft.AspNetCore.Mvc;
using SuperReservationSystem.Models;
using System.Diagnostics;
using BusinessLayer.Services;

namespace SuperReservationSystem.Controllers
{
    /// <summary>
    /// Controller for managing the home page and server management.
    /// </summary>
    public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private ServerService serverService = new ServerService();

        /// <summary>
        /// Constructor for the HomeController class.
        /// </summary>
        /// <param name="logger"></param>
        public HomeController(ILogger<HomeController> logger)
		{
			_logger = logger;
		}

        /// <summary>
        /// Displays the main page of the application.
        /// </summary>
        /// <returns>  An <see cref="IActionResult"/> that renders home page </returns>
        public IActionResult Index()
		{			
			if (User.Identity != null && !User.Identity.IsAuthenticated)
				return RedirectToAction("Index","Login");			
            ViewBag.Servers = serverService.GetAllServers();
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