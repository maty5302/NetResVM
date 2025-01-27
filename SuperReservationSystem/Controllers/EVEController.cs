using BusinessLayer.Services;
using BusinessLayer.Services.ApiEVEServices;
using Microsoft.AspNetCore.Mvc;

namespace SuperReservationSystem.Controllers
{
    public class EVEController : Controller
    {
        private readonly ApiEVELabService _apiEVELabService = new ApiEVELabService();
        private readonly ServerService serverService = new ServerService();

        public IActionResult Index(int id)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Access denied. Log in to use this feature.";
                return RedirectToAction("Login", "Home");
            }
            return RedirectToAction("LabList", "EVE", new { id = id });
        }

        public async Task<IActionResult> LabList(int id)
        {
            var server = serverService.GetServerById(id);
            if (server == null)
            {
                TempData["ErrorMessage"] = "Server not found.";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ServerName = server.Name;
            ViewBag.ServerID = id;
            var labs = await _apiEVELabService.GetLabs(id);
            if (labs != null)
            {
                ViewBag.Labs = labs;
                return View();
            }
            TempData["ErrorMessage"] = "An error occurred. Please try again.";
            return RedirectToAction("Index", "Home");
        }
        public async Task<IActionResult> LabInfo(int id, string filename)
        {
            if (filename == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("LabList", "CML", new { id = id });
            }
            ViewBag.ServerID = id;

            var lab = await _apiEVELabService.GetLabInfo(id, filename);
            if (lab == null)
            {
                TempData["ErrorMessage"] = "";
                return RedirectToAction("LabList", "CML", new { id = id });
            }
            ViewBag.Lab = lab;
            return View();
        }
    }
}
