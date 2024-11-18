using Microsoft.AspNetCore.Mvc;
using SuperReservationSystem.Models;
using System.Diagnostics;
using ApiCisco;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace SuperReservationSystem.Controllers
{
    public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
		{
			_logger = logger;
		}

		public IActionResult Index()
		{			
			if (!User.Identity.IsAuthenticated)
				return RedirectToAction("Login");
            ViewBag.Servers = TempFakeDatabase.Servers;
            return View();
		}

		public IActionResult Privacy()
		{
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login");
            return View();
		}

        //[Authorize(Roles = "Admin")]
        public IActionResult AddServer()
		{
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login");
			if (!User.IsInRole("Admin"))
				return RedirectToAction("Index");
            //only admin
            return View();
		}

		public IActionResult Login()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> LoginAsync(LoginModel user/*string username, string password*/)
		{
			if (ModelState.IsValid)
			{
				if ((user.Username == null || user.Password == null) /*a zaroven platny login*/ )
				{

					var claims = new List<Claim>
					{
					new Claim(ClaimTypes.Name, "admin"),
					new Claim(ClaimTypes.Role, "Admin") // Assuming "admin" has Admin role
					};
					var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

					await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

					return RedirectToAction("Index", "Home");
				}
			}
			TempData["ErrorMessage"] = "Invalid credentials";
            return View();
        }
		
		public async Task<IActionResult> Logout()
		{
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Home");
		}

		public async Task<IActionResult> TestConnection(ServerModel server)
		{
			if(!ModelState.IsValid)
			{
				return View("Error");
			}
			var client = new UserHttpClient(server.IpAddress);
			var res = await Authentication.Authenticate(client, server.Username, server.Password);
			if (res != null && res.IsSuccessStatusCode)
			{
				TempData["SuccessMessage"] = "Connection successful";
				//enable button for Add Server
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
                var client = new UserHttpClient(server.IpAddress);
                var res = await Authentication.Authenticate(client, server.Username, server.Password);
                if (res != null && res.IsSuccessStatusCode)
				{
                    //TODO: Save to database
                    TempFakeDatabase.Servers.Add(server);
                    ViewBag.Servers = TempFakeDatabase.Servers;
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