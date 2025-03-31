using BusinessLayer.Services;
using BusinessLayer.Services.ApiEVEServices;
using Microsoft.AspNetCore.Mvc;

namespace SuperReservationSystem.Controllers
{
    public class EVEController : Controller
    {
        private readonly ApiEVELabService _apiEVELabService = new ApiEVELabService();
        private readonly ApiEVENodeService _apiEVENodeService = new ApiEVENodeService();
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
        public async Task<IActionResult> DownloadLab(int id, string labId)
        {
            if (labId == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("LabList", "EVE", new { id = id });
            }
            var lab = await _apiEVELabService.GetLabInfoById(id, labId);
            if (lab == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("LabList", "EVE", new { id = id });
            }
            var file = await _apiEVELabService.DownloadLab(lab, id);
            if (file == null)
            {
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
                return RedirectToAction("LabList", "EVE", new { id = id });
            }
            return File(file, "application/zip", $"{lab.Name}-{DateTime.Now}.zip");
        }
        public async Task<IActionResult> ImportLab(IFormFile file, int serverId)
        {
            if (file == null)
            {
                TempData["ErrorMessage"] = "No file selected.";
                return RedirectToAction("LabList", "EVE", new { id = serverId });
            }
            if (file.Length == 0)
            {
                TempData["ErrorMessage"] = "The file is empty.";
                return RedirectToAction("LabList", "EVE", new { id = serverId });
            }
            var result = await _apiEVELabService.ImportLab(serverId, file);
            if (!result)
            {
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
                return RedirectToAction("LabList", "EVE", new { id = serverId });
            }
            return RedirectToAction("LabList", "EVE", new { id = serverId });
        }

        public async Task<IActionResult> LabNodeList(int serverId, string labId)
        {
            if (labId == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("LabList", "EVE", new { id = serverId });
            }
            var lab = await _apiEVELabService.GetLabInfoById(serverId, labId);
            if (lab == null)
            {
                TempData["ErrorMessage"] = "";
                return RedirectToAction("LabList", "EVE", new { id = serverId });
            }
            var nodes = await _apiEVENodeService.GetAllNodes(serverId, lab.Filename);
            ViewBag.Title = lab.Name + " nodes";
            ViewBag.Nodes = nodes;
            return View();
        }


        public async Task<IActionResult> StartLab(int serverId, string labId)
        {
            if (labId == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("LabList", "EVE", new { id = serverId });
            }
            var lab = await _apiEVELabService.GetLabInfoById(serverId, labId);
            if (lab == null)
            {
                return RedirectToAction("LabList", "EVE", new { id = serverId });
            }
            var res = await _apiEVENodeService.StartAllNodes(serverId, lab.Filename);
            if (!res)
            {
                TempData["ErrorMessage"] = "Couldn't start a lab";
            }
            else
            {
                TempData["SuccessMessage"] = "Lab started";
            }
            return RedirectToAction("LabInfo", "EVE", new { id = serverId , labId = labId });
        }

        public async Task<IActionResult> StopLab(int serverId, string labId)
        {
            if (labId == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("LabList", "EVE", new { id = serverId });
            }
            var lab = await _apiEVELabService.GetLabInfoById(serverId, labId);
            if (lab == null)
            {
                return RedirectToAction("LabList", "EVE", new { id = serverId });
            }
            var res = await _apiEVENodeService.StopAllNodes(serverId, lab.Filename);
            if (!res)
            {
                TempData["ErrorMessage"] = "Couldn't start a lab";
            }
            else
            {
                TempData["SuccessMessage"] = "Lab started";
            }
            return RedirectToAction("LabInfo", "EVE", new { id = serverId, labId = labId });
        }
    }
}
