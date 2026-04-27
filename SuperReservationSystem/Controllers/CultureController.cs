using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace NetResVM.Controllers
{
    public class CultureController : Controller
    {
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