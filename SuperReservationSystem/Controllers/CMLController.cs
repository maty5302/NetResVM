using BusinessLayer.Services;
using BusinessLayer.Services.ApiCiscoServices;
using Microsoft.AspNetCore.Mvc;

namespace SuperReservationSystem.Controllers
{
    /// <summary>
    /// Controller for managing Cisco Modeling Labs (CML) functionalities.
    /// </summary>
    public class CMLController : Controller
    {
        private readonly UserLabOwnershipService labOwnershipService = new UserLabOwnershipService();
        private readonly UserService userService = new UserService();
        private readonly ServerService serverService = new ServerService();
        private readonly ApiCiscoAuthService authService = new ApiCiscoAuthService();
        private readonly ApiCiscoLabService labService = new ApiCiscoLabService();
        private readonly ApiCiscoNodeService nodeService = new ApiCiscoNodeService();

        /// <summary>
        /// Displays the main page of the CML application.
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
            return RedirectToAction("LabList", "CML", new { id = id });
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
            if (server==null)
            {
                TempData["ErrorMessage"] = "Server not found.";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ServerName = server.Name;
            ViewBag.ServerID = id;
         
            var labs = await labService.GetLabs(id);
            if(labs.labs!=null)
            {
                ViewBag.Labs = labs.labs;
                return View();
            }

            TempData["ErrorMessage"] = $"An error occurred. {labs.Message}. Please try again.";
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
                return RedirectToAction("LabList", "CML", new { id = id });
            }

            ViewBag.ServerID = id;
            // Check if the lab is already owned
            var owned = labOwnershipService.IsLabAlreadyOwned(userService.GetUserId(User.Identity?.Name),labId);

            ViewBag.Owned = owned.owned;
            ViewBag.UserOwn = owned.userOwns;
            var lab = await labService.GetLabInfo(id, labId);
            if (lab.lab == null)
            {
                TempData["ErrorMessage"] = lab.Message;
                return RedirectToAction("LabList", "CML", new { id = id });
            }
            ViewBag.Lab = lab.lab;
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
                return RedirectToAction("LabList", "CML", new { id = id });
            }
            ViewBag.ServerID = id;

            var data = await labService.DownloadLab(id, labId);
            if (data.fileContent == null)
            {
                TempData["ErrorMessage"] = data.message;
                return RedirectToAction("LabList", "CML", new { id = id });
            }
            return File(data.fileContent, "text/plain", "lab.yaml");
        }

        /// <summary>
        /// Starts a lab.
        /// </summary>
        /// <param name="id"> ID of the server </param>
        /// <param name="labId"> ID of the lab to start </param>
        /// <returns>  An <see cref="Task{IActionResult}"/> that renders LabInfo or redirects to Home if something is wrong </returns>
        public async Task<IActionResult> StartLab(int id, string labId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            if (labId == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("Index", "Home");
            }
            var result = await labService.StartLab(id, labId);
            if (result.value)
            {
                TempData["SuccessMessage"] = "Lab started successfully.";
                return RedirectToAction("LabInfo", "CML", new { id = id, labId = labId });
            }
            TempData["ErrorMessage"] = result.message;
            return RedirectToAction("LabInfo", "CML", new { id = id, labId = labId });
        }

        /// <summary>
        /// Stops a lab.
        /// </summary>
        /// <param name="id"> ID of the server </param>
        /// <param name="labId"> ID of the lab to stop </param>
        /// <returns>  An <see cref="Task{IActionResult}"/> that renders LabInfo or redirects to home or lablist if something is wrong </returns>
        public async Task<IActionResult> StopLab(int id, string labId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            if (labId == null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("Index", "Home");
            }
            var result =await labService.StopLab(id, labId);
            if (result.value)
            {
                TempData["SuccessMessage"] = "Lab stopped successfully.";
                return RedirectToAction("LabInfo", "CML", new { id = id, labId = labId });
            }
            TempData["ErrorMessage"] = result.message;
            return RedirectToAction("LabList", "CML", new { id = id });
        }

        /// <summary>
        /// Imports a lab configuration file.
        /// </summary>
        /// <param name="file"> File containing lab to be imported (YAML) </param>
        /// <param name="serverId"> ID of the server where to import </param>
        /// <returns> An <see cref="Task{IActionResult}"/> that renders LabList and gives a message about success or failure </returns>
        [HttpPost]
        public async Task<IActionResult> ImportLab(IFormFile file, int serverId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            if (file == null)
            {
                TempData["ErrorMessage"] = "File not found.";
                return RedirectToAction("LabList", "CML", new { id = serverId });
            }
            var res = await labService.ImportLab(serverId,file);
            if (res)
            {
                TempData["SuccessMessage"] = "Lab imported successfully.";
                return RedirectToAction("LabList", "CML", new { id = serverId });
            }
            TempData["ErrorMessage"] = "An error occurred.";
            return RedirectToAction("LabList", "CML", new { id = serverId });

        }

        /// <summary>
        /// Deletes a lab.
        /// </summary>
        /// <param name="serverId"> ID of the server where to delete a lab</param>
        /// <param name="labId"> ID of the lab to be deleted </param>
        /// <returns> An <see cref="Task{IActionResult}"/> that renders LabList with message </returns>
        public async Task<IActionResult> DeleteLab(int serverId, string labId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            if (labId==null)
            {
                TempData["ErrorMessage"] = "Lab not found.";
                return RedirectToAction("LabList", "CML", new { id = serverId });
            }
            var result = await labService.DeleteLab(serverId, labId);
            if (result.value)
            {
                TempData["SuccessMessage"] = result.message;
                return RedirectToAction("LabList", "CML", new { id = serverId });
            }
            TempData["ErrorMessage"] = result.message;
            return RedirectToAction("LabList", "CML", new { id = serverId });
        }

        /// <summary>
        /// Displays the list of nodes for a specific lab.
        /// </summary>
        /// <param name="serverId"> ID of the server </param>
        /// <param name="labId"> ID of the lab </param>
        /// <returns>  An <see  cref="Task{IActionResult}"/> that renders LabNodeList or Index/LabInfo if something goes wrong </returns>
        public async Task<IActionResult> LabNodeList(int serverId, string labId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            var client = await authService.AuthenticateAndCreateClient(serverId);
            if (client.conn == null)
            {
                TempData["ErrorMessage"] = client.message;
                return RedirectToAction("Index", "Home");
            }
            var nodes = await nodeService.GetNodes(serverId, labId);
            if (nodes != null)
            {
                ViewBag.Nodes = nodes;
                return View();
            }
            TempData["ErrorMessage"] = "An error occurred.";
            return RedirectToAction("LabInfo", "CML", new { id = serverId, labId = labId });

        }
    }
}
