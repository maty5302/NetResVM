using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using SuperReservationSystem.Models;
using System.Security.Claims;
using BusinessLayer.Services;

namespace SuperReservationSystem.Controllers
{
    /// <summary>
    /// Controller for managing user login and authentication.
    /// </summary>
    public class LoginController : Controller
    {
        private UserService userService = new UserService();

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
                if (userService.ValidateCredentials(user.Username,user.Password))
                {
                    // Create the claims for the user
                    var claims = new List<Claim>
                    {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, userService.GetRole(user.Username)) 
					};
                    // Create the claims identity and sign in the user
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    // Set the authentication properties
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Home");
                }
            }
            TempData["ErrorMessage"] = "Invalid credentials";
            return View("Index");
        }

        /// <summary>
        /// Handles the logout process.
        /// </summary>
        /// <returns> An <see cref="Task{IActionResult}"/> that renders login page and sing out user</returns>
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }
    }
}
