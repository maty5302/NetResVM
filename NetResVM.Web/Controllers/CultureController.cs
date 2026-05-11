using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace NetResVM.Web.Controllers
{
    /// <summary>
    /// /// Controller responsible for handling culture and language settings for the application.
    /// </summary>
    public class CultureController : Controller
    {
        /// <summary>
        /// Sets the user's preferred UI culture and redirects to the specified return URL.
        /// </summary>
        /// <remarks>The selected culture is stored in a cookie that persists for one year. This method is
        /// typically used to support localization by allowing users to change the application's language.</remarks>
        /// <param name="culture">The culture name to set for the current user, such as "en-US" or "cs-CZ". Must be a valid culture
        /// identifier.</param>
        /// <param name="returnUrl">The URL to redirect the user to after setting the culture. If null or empty, the user is redirected to the
        /// application's root.</param>
        /// <returns>A redirect result to the specified return URL or the application's root if no return URL is provided.</returns>
        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    Path = "/"
                }
            );

            // Pokud returnUrl chybí, hodíme ho na hlavní stránku
            return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
        }
    }
}