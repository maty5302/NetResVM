using System.Security.Claims;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NetResVM.Web.Models;
using SuperReservationSystem.Models;

namespace NetResVM.Web.Controllers
{
    /// <summary>
    /// Controller for managing user login and authentication.
    /// </summary>
    public class LoginController : Controller
    {
        private UserService _userService;
        private readonly ILogger<LoginController> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public LoginController(UserService userService, ILogger<LoginController> logger, IStringLocalizer<SharedResource> localizer)
        {
            _userService = userService;
            _logger = logger;
            _localizer = localizer;
        }

        /// <summary>
        /// Displays the login page.
        /// </summary>
        /// <returns>  An <see cref="IActionResult"/> that renders login page </returns>
        public IActionResult Index()
        {
            if(User.Identity != null && User.Identity.IsAuthenticated) {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        /// <summary>
        /// Handles the login process.
        /// </summary>
        /// <param name="user"> Model where username and password is stored for verify</param>
        /// <returns>  An <see cref="Task{IActionResult}"/> that renders home or login page and message about success or failure of operation </returns>
        [HttpPost]
        public async Task<IActionResult> LoginASync(LoginModel user)
        {
            if (ModelState.IsValid && !User.Identity.IsAuthenticated)
            {
                // Check if the user is already authenticated
                if (_userService.ValidateCredentials(user.Username,user.Password))
                {
                    // Create the claims for the user
                    var claims = new List<Claim>
                    {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, _userService.GetRole(user.Username)) 
					};
                    // Create the claims identity and sign in the user
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    // Set the authentication properties
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Home");
                }
            }
            TempData["ErrorMessage"] = _localizer["InvalidCredentials"].Value;
            return View("Index");
        }

        /// <summary>
        /// Handles the logout process.
        /// </summary>
        /// <returns> An <see cref="Task{IActionResult}"/> that renders login page and signs out user</returns>
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }
    }
}
