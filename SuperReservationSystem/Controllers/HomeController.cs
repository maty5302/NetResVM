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
        /// Displays the main page of the application.
        /// </summary>
        /// <returns>  An <see cref="IActionResult"/> that renders home page </returns>
        public IActionResult Index()
		{			
			if (User.Identity != null && !User.Identity.IsAuthenticated)
				return RedirectToAction("Index","Login");			
            ViewBag.Servers = _serverService.GetAllServers();
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