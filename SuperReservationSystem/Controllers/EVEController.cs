using BusinessLayer.Services;
using BusinessLayer.Services.ApiEVEServices;
using Microsoft.AspNetCore.Mvc;

namespace SuperReservationSystem.Controllers
{
    /// <summary>
    /// Controller for managing EVE labs and nodes.
    /// </summary>
    public class EVEController : Controller
    {
        private readonly ApiEVELabService _apiEVELabService = new ApiEVELabService();
        private readonly ApiEVENodeService _apiEVENodeService = new ApiEVENodeService();
        private readonly ServerService serverService = new ServerService();
        private readonly UserLabOwnershipService labOwnershipService = new UserLabOwnershipService();
        private readonly UserService userService = new UserService();

        /// <summary>
        /// Displays the main page of the EVE controller.
        /// </summary>
        /// <param name="id"> ID of the server </param>
        /// <returns> An <see cref="IActionResult"/> that redirects to LabList if id exists </returns>
        public IActionResult Index(int id)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Access denied. Log in to use this feature.";
                return RedirectToAction("Login", "Home");
            }
            return RedirectToAction("LabList", "EVE", new { id = id });
        }

        /// <summary>
        /// Displays the list of labs for a specific server.
        /// </summary>
        /// <param name="id"> ID of the server</param>
        /// <returns>  An <see cref="Task{IActionResult}"/> that renders LabList or redirects to home if something is wrong </returns>
        public async Task<IActionResult> LabList(int id)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
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

        /// <summary>
        /// Displays the information about a specific lab.
        /// </summary>
        /// <param name="id"> ID of the server </param>
        /// <param name="labId"> ID of the lab to get info for</param>
        /// <returns>  An <see cref="Task{IActionResult}"/> that renders LabInfo or redirects to LabList if something is wrong </returns>
        public async Task<IActionResult> LabInfo(int id, string labId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            if (labId == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("LabList", "EVE", new { id = id });
            }
            // Check if the lab is already owned
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

        /// <summary>
        /// Downloads the lab configuration file.
        /// </summary>
        /// <param name="id"> ID of the server </param>
        /// <param name="labId"> ID of the lab to get download for</param>
        /// <returns>  An <see cref="Task{IActionResult}"/> that renders LabInfo or redirects to LabList if something is wrong </returns>
        public async Task<IActionResult> DownloadLab(int id, string labId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
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

        /// <summary>
        /// Imports a lab configuration file.
        /// </summary>
        /// <param name="file"> File containing lab to be imported (ZIP) </param>
        /// <param name="serverId"> ID of the server where to import </param>
        /// <returns> An <see cref="Task{IActionResult}"/> that renders LabList and gives a message about success or failure </returns>
        public async Task<IActionResult> ImportLab(IFormFile file, int serverId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
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

        /// <summary>
        /// Displays the list of nodes for a specific lab.
        /// </summary>
        /// <param name="serverId"> ID of the server </param>
        /// <param name="labId"> ID of the lab </param>
        /// <returns>  An <see  cref="Task{IActionResult}"/> that renders LabNodeList or LabList if something goes wrong </returns>
        public async Task<IActionResult> LabNodeList(int serverId, string labId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
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

        /// <summary>
        /// Starts a lab.
        /// </summary>
        /// <param name="serverId"> ID of the server </param>
        /// <param name="labId"> ID of the lab to start </param>
        /// <returns>  An <see cref="Task{IActionResult}"/> that renders LabInfo or redirects to LabList if something is wrong </returns>
        public async Task<IActionResult> StartLab(int serverId, string labId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
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

        /// <summary>
        /// Stops a lab.
        /// </summary>
        /// <param name="serverId"> ID of the server </param>
        /// <param name="labId"> ID of the lab to stop </param>
        /// <returns>  An <see cref="Task{IActionResult}"/> that renders LabInfo or redirects to home or lablist if something is wrong </returns>
        public async Task<IActionResult> StopLab(int serverId, string labId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
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
                TempData["ErrorMessage"] = "Couldn't stop a lab";
            }
            else
            {
                TempData["SuccessMessage"] = "Lab stopped";
            }
            return RedirectToAction("LabInfo", "EVE", new { id = serverId, labId = labId });
        }

        /// <summary>
        /// Deletes a lab.
        /// </summary>
        /// <param name="serverId"> ID of the server </param>
        /// <param name="labId"> ID of the lab to stop </param>
        /// <returns>  An <see cref="Task{IActionResult}"/> that renders LabList and tells if operation was successful </returns>
        public async Task<IActionResult> DeleteLab(int serverId, string labId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            if (labId == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("LabList", "EVE", new { id = serverId });
            }
            var res = await _apiEVELabService.DeleteLab(serverId, labId);
            if (!res)
            {
                TempData["ErrorMessage"] = "Couldn't delete a lab";
            }
            else
            {
                TempData["SuccessMessage"] = "Lab deleted";
            }
            return RedirectToAction("LabList", "EVE", new { id = serverId });
        }
    }
}
