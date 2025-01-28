using BusinessLayer.Services;
using BusinessLayer.Services.ApiEVEServices;
using Microsoft.AspNetCore.Mvc;

namespace SuperReservationSystem.Controllers
{
    public class EVEController : Controller
    {
        private readonly ApiEVELabService _apiEVELabService = new ApiEVELabService();
        private readonly ServerService serverService = new ServerService();
        private readonly UserLabOwnershipService labOwnershipService = new UserLabOwnershipService();
        private readonly UserService userService = new UserService();

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
        public async Task<IActionResult> LabInfo(int id, string labId)
        {
            if (labId == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("LabList", "EVE", new { id = id });
            }
            var owned = labOwnershipService.IsLabAlreadyOwned(userService.GetUserId(User.Identity?.Name), labId);
            ViewBag.Owned = owned.owned;
            ViewBag.UserOwn = owned.userOwns;
            ViewBag.ServerID = id;

            var lab = await _apiEVELabService.GetLabInfoById(id, labId);
            if (lab == null)
            {
                TempData["ErrorMessage"] = "";
                return RedirectToAction("LabList", "EVE", new { id = id });
            }
            ViewBag.Lab = lab;
            return View();
        }
    }
}
