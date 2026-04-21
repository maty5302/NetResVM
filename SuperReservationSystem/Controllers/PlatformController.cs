using BusinessLayer.Enum;
using BusinessLayer.Interface;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.DTOs;

namespace NetResVM.Controllers
{
    public class PlatformController : Controller
    {
        private readonly PlatformManager _platformManager;
        private readonly ServerService _serverService;
        private readonly UserService _userService;
        private readonly UserLabOwnershipService _userLabOwnership;

        public PlatformController(PlatformManager platformManager, ServerService serverService, UserService userService, UserLabOwnershipService userLabOwnership)
        {
            _platformManager = platformManager;
            _serverService = serverService;
            _userService = userService;
            _userLabOwnership = userLabOwnership;
        }

        /// <summary>
        /// Redirects the user to the lab list page for the specified server, or to the login page if the user is not
        /// authenticated.
        /// </summary>
        /// <remarks>If the user is not authenticated, an error message is set in TempData before
        /// redirecting to the login page.</remarks>
        /// <param name="serverId">The unique identifier of the server for which to display the lab list.</param>
        /// <returns>A redirect result to the lab list page if the user is authenticated; otherwise, a redirect result to the
        /// login page.</returns>
        public IActionResult Index(int serverId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Access denied. Log in to use this feature.";
                return RedirectToAction("Login", "Home");
            }
            return RedirectToAction("LabList", "Platform", new { serverId = serverId });
        }


        /// <summary>
        /// Displays a list of labs available on the specified server.
        /// </summary>
        /// <remarks>If the user is not authenticated, the method redirects to the login page. If the
        /// server is not found or the platform is unsupported, or if authentication fails, an error message is set and
        /// the user is redirected to the home page. The returned view contains the list of labs for the specified
        /// server, or an empty list if no labs are found.</remarks>
        /// <param name="serverId">The unique identifier of the server from which to retrieve the list of labs.</param>
        /// <returns>An <see cref="IActionResult"/> that renders the lab list view if successful; otherwise, a redirect to the
        /// appropriate page with an error message if the server is not found, authentication fails, or the platform is
        /// unsupported.</returns>
        public async Task<IActionResult> LabList(int serverId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Home");
            }

            var server = _serverService.GetServerById(serverId);
            if (server == null)
            {
                TempData["ErrorMessage"] = "Server not found.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                if (server.Platform == PlatformType.Unknown)
                {
                    TempData["ErrorMessage"] = "Unsupported platform type.";
                    return RedirectToAction("Index", "Home");
                }
                IVirtualizationAdapter adapter = _platformManager.GetAdapter(server.Platform);

                var response = await adapter.AuthenticateAsync(server.Id);
                if (!response.Valid)
                {
                    TempData["ErrorMessage"] = $"Authentication failed: {response.Message}";
                    return RedirectToAction("Index", "Home");
                }

                var labsResult = await adapter.GetLabsAsync(serverId);
                if (labsResult.Labs == null)
                {
                    return View(new List<LabDTO>());
                }
                ViewBag.ServerId = serverId;
                ViewBag.ServerName = server.Name;
                ViewBag.PlatformName = adapter.PlatformName.ToString();
                return View(labsResult.Labs);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }


        /// <summary>
        /// Displays detailed information about a specific lab on the selected server.
        /// </summary>
        /// <remarks>Redirects to the login page if the user is not authenticated. If the server or lab is
        /// not found, or if authentication fails, the user is redirected with an error message. Only supported platform
        /// types are processed.</remarks>
        /// <param name="serverId">The unique identifier of the server hosting the lab.</param>
        /// <param name="labId">The unique identifier of the lab to display information for.</param>
        /// <returns>A view displaying the lab details if found and accessible; otherwise, a redirect to an appropriate page with
        /// an error message.</returns>
        public async Task<IActionResult> LabInfo(int serverId, string labId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Home");
            }

            var server = _serverService.GetServerById(serverId);
            if (server == null)
            {
                TempData["ErrorMessage"] = "Server not found.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                if (server.Platform == PlatformType.Unknown)
                {
                    TempData["ErrorMessage"] = "Unsupported platform type.";
                    return RedirectToAction("Index", "Home");
                }
                IVirtualizationAdapter adapter = _platformManager.GetAdapter(server.Platform);

                var response = await adapter.AuthenticateAsync(server.Id);
                if (!response.Valid)
                {
                    TempData["ErrorMessage"] = $"Authentication failed: {response.Message}";
                    return RedirectToAction("Index", "Home");
                }

                var labInfoResult = await adapter.GetLabInfoAsync(serverId,labId);
                if (labInfoResult.Lab == null)
                {
                    TempData["ErrorMessage"] = $"Lab with ID {labId} not found.";
                    return RedirectToAction("LabList", "Platform", new { serverId = serverId });
                }
                var owned = _userLabOwnership.IsLabAlreadyOwned(_userService.GetUserId(User.Identity.Name), labId);

                ViewBag.ServerId = serverId;
                ViewBag.ServerName = server.Name;
                ViewBag.PlatformName = adapter.PlatformName.ToString();
                ViewBag.Owned = owned.owned;
            ViewBag.UserOwn = owned.userOwns;
                return View(labInfoResult.Lab);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Imports a lab configuration file.
        /// </summary>
        /// <param name="file"> File containing lab to be imported (YAML(CML)/ZIP(EVE)) </param>
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
                return RedirectToAction("LabList", "Platform", new { serverId = serverId });
            }
            var server = _serverService.GetServerById(serverId);
            if (server == null)
            {
                TempData["ErrorMessage"] = "Server not found.";
                return RedirectToAction("Index", "Home");
            }
            if (server.Platform == PlatformType.Unknown)
            {
                TempData["ErrorMessage"] = "Unsupported platform type.";
                return RedirectToAction("Index", "Home");
            }
            IVirtualizationAdapter adapter = _platformManager.GetAdapter(server.Platform);
            var res = await adapter.ImportLab(serverId, file);
            if (res)
            {
                TempData["SuccessMessage"] = "Lab imported successfully.";
                return RedirectToAction("LabList", "Platform", new { serverId = serverId });
            }
            TempData["ErrorMessage"] = "An error occurred.";
            return RedirectToAction("LabList", "Platform", new { serverId = serverId });

        }

        /// <summary>
        /// Asynchronously downloads a virtualization lab topology or export file from the specified server.
        /// The file format (e.g., .yaml for CML, .zip for EVE-NG) is automatically determined by the underlying platform adapter.
        /// </summary>
        /// <param name="serverId">The unique identifier of the target server where the lab is hosted.</param>
        /// <param name="labId">The unique identifier of the specific lab to be downloaded.</param>
        /// <param name="labDto">An optional data transfer object containing lab metadata, typically used to generate a safe and descriptive filename for the download.</param>
        /// <returns>
        /// A file download response (<see cref="FileContentResult"/>) containing the lab data upon success, 
        /// or a redirection to the lab list view with an error message if the download fails or the server is not found.
        /// </returns>
        public async Task<IActionResult> DownloadLab(int serverId, string? labId, LabDTO? labDto)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            var server = _serverService.GetServerById(serverId);
            if (server == null)
            {
                TempData["ErrorMessage"] = "Server not found.";
                return RedirectToAction("Index", "Home");
            }
            if (server.Platform == PlatformType.Unknown)
            {
                TempData["ErrorMessage"] = "Unsupported platform type.";
                return RedirectToAction("Index", "Home");
            }
            IVirtualizationAdapter adapter = _platformManager.GetAdapter(server.Platform);
            ViewBag.ServerID = serverId;
            if (labDto == null || string.IsNullOrEmpty(labDto.Id))
            {
                var labResult = await adapter.GetLabInfoAsync(serverId, labId);
                labDto = labResult.Lab;
            }
            var data = await adapter.DownloadLab(serverId, labId, labDto);
            if (data.FileContent == null)
            {
                TempData["ErrorMessage"] = data.Message;
                return RedirectToAction("LabList", "Platform", new { serverId = serverId });
            }
            return File(data.FileContent, data.ContentType, data.FileName);
        }

        /// <summary>
        /// Deletes a lab identified by the specified server and lab identifiers and redirects to the lab list view.
        /// </summary>
        /// <remarks>If the user is not authenticated, the method redirects to the login page. If the
        /// server is not found or the platform type is unsupported, an error message is displayed and the user is
        /// redirected to the home page. Success or error messages are set in TempData based on the result of the
        /// deletion.</remarks>
        /// <param name="serverId">The unique identifier of the server that hosts the lab to be deleted.</param>
        /// <param name="labId">The unique identifier of the lab to delete.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="IActionResult"/>
        /// that redirects the user to the appropriate view based on the outcome of the deletion.</returns>
        public async Task<IActionResult> DeleteLab(int serverId, string labId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            var server = _serverService.GetServerById(serverId);
            if (server == null)
            {
                TempData["ErrorMessage"] = "Server not found.";
                return RedirectToAction("Index", "Home");
            }
            if (server.Platform == PlatformType.Unknown)
            {
                TempData["ErrorMessage"] = "Unsupported platform type.";
                return RedirectToAction("Index", "Home");
            }
            IVirtualizationAdapter adapter = _platformManager.GetAdapter(server.Platform);
            var result = await adapter.DeleteLab(serverId, labId);
            if (result.value)
            {
                TempData["SuccessMessage"] = "Lab deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = result.message ?? "An error occurred while deleting the lab.";
            }
            return RedirectToAction("LabList", "Platform", new { serverId = serverId });
        }

        /// <summary>
        /// Starts the specified lab on the given server and redirects to the lab information page or an appropriate
        /// error page.
        /// </summary>
        /// <remarks>If the user is not authenticated, the method redirects to the login page. If the
        /// server is not found or the platform type is unsupported, the method redirects to the home page with an error
        /// message. Success and error messages are set using TempData for display on the redirected page.</remarks>
        /// <param name="serverId">The unique identifier of the server on which to start the lab.</param>
        /// <param name="labId">The identifier of the lab to be started.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult that redirects
        /// to the lab information page if the operation succeeds, or to an error page if the operation fails.</returns>
        public async Task<IActionResult> StartLab(int serverId, string labId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            var server = _serverService.GetServerById(serverId);
            if (server == null)
            {
                TempData["ErrorMessage"] = "Server not found.";
                return RedirectToAction("Index", "Home");
            }
            if (server.Platform == PlatformType.Unknown)
            {
                TempData["ErrorMessage"] = "Unsupported platform type.";
                return RedirectToAction("Index", "Home");
            }
            IVirtualizationAdapter adapter = _platformManager.GetAdapter(server.Platform);
            var result = await adapter.StartLabAsync(serverId, labId);
            if (result.value)
            {
                TempData["SuccessMessage"] = "Lab started successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = result.message ?? "An error occurred while starting the lab.";
            }
            return RedirectToAction("LabInfo", "Platform", new { serverId = serverId, labId = labId });
        }

        /// <summary>
        /// Stops the specified lab on the given server and redirects to the lab information page with a status message.
        /// </summary>
        /// <remarks>If the user is not authenticated, the method redirects to the login page. If the
        /// server is not found or the platform type is unsupported, the user is redirected to the home page with an
        /// error message. The result of the stop operation is communicated to the user via a status message.</remarks>
        /// <param name="serverId">The unique identifier of the server hosting the lab to be stopped.</param>
        /// <param name="labId">The identifier of the lab to stop on the specified server.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="IActionResult"/>
        /// that redirects to the lab information page with a success or error message.</returns>
        public async Task<IActionResult> StopLab(int serverId, string labId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            var server = _serverService.GetServerById(serverId);
            if (server == null)
            {
                TempData["ErrorMessage"] = "Server not found.";
                return RedirectToAction("Index", "Home");
            }
            if (server.Platform == PlatformType.Unknown)
            {
                TempData["ErrorMessage"] = "Unsupported platform type.";
                return RedirectToAction("Index", "Home");
            }
            IVirtualizationAdapter adapter = _platformManager.GetAdapter(server.Platform);
            var result = await adapter.StopLabAsync(serverId, labId);
            if (result.value)
            {
                TempData["SuccessMessage"] = "Lab stopped successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = result.message ?? "An error occurred while stopping the lab.";
            }
            return RedirectToAction("LabInfo", "Platform", new { serverId = serverId, labId = labId });
        }

        /// <summary>
        /// Retrieves and displays the list of nodes for a specified lab on a given server.
        /// </summary>
        /// <remarks>Redirects to the login page if the user is not authenticated. If the server is not
        /// found or the platform is unsupported, the user is redirected to the home page with an error message. If node
        /// retrieval fails, the user is redirected to the lab information page with an error message.</remarks>
        /// <param name="serverId">The unique identifier of the server hosting the lab. Must correspond to an existing server.</param>
        /// <param name="labId">The identifier of the lab whose nodes are to be listed. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult that renders
        /// the node list view if successful, or redirects to an appropriate page with an error message if the operation
        /// fails or the user is not authenticated.</returns>
        public async Task<IActionResult> LabNodeList(int serverId, string labId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            var server = _serverService.GetServerById(serverId);
            if (server == null)
            {
                TempData["ErrorMessage"] = "Server not found.";
                return RedirectToAction("Index", "Home");
            }
            if (server.Platform == PlatformType.Unknown)
            {
                TempData["ErrorMessage"] = "Unsupported platform type.";
                return RedirectToAction("Index", "Home");
            }
            IVirtualizationAdapter adapter = _platformManager.GetAdapter(server.Platform);
            var result = await adapter.GetAllNodes(serverId, labId);
            if (result.Nodes == null)
            {
                TempData["ErrorMessage"] = result.Message ?? "An error occurred while retrieving nodes.";
                return RedirectToAction("LabInfo", "Platform", new { serverId = serverId, labId = labId });
            }
            return View(result.Nodes);
        }
    }
}

