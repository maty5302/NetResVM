using BusinessLayer.MapperDT;
using BusinessLayer.Models;
using BusinessLayer.Services;
using BusinessLayer.Services.ApiCiscoServices;
using BusinessLayer.Services.ApiEVEServices;
using Microsoft.AspNetCore.Mvc;

namespace SuperReservationSystem.Controllers
{
    /// <summary>
    /// Controller for managing servers.
    /// </summary>
    public class ServerController : Controller
    {
        private ServerService serverService = new ServerService();
        private ApiCiscoAuthService authServiceCisco = new ApiCiscoAuthService();
        private ApiEVEAuthService authServiceEVE = new ApiEVEAuthService();

        /// <summary>
        /// Displays the list of servers.
        /// </summary>
        /// <returns> An <see cref="IActionResult"/> that renders Add page</returns>
        public IActionResult Add()
        {
            if (User.Identity!=null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Index", "Home");

            return View();
        }

        /// <summary>
        /// Displays the edit view for a specific server.
        /// </summary>
        /// <param name="id"> ID of server to edit</param>
        /// <returns> An <see cref="IActionResult"/> that renders edit page  </returns>
        public IActionResult Edit(int id)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Index", "Home");

            var server = serverService.GetServerById(id);
            if (server == null)
            {
                TempData["ErrorMessage"] = "Server not found.";
                return RedirectToAction("Index", "Home");
            }

            return View(ServerMapper.ToModel(server));
        }

        /// <summary>
        /// Removes a server by its ID.
        /// </summary>
        /// <param name="id"> ID of the server to remove </param>
        /// <returns> An <see cref="IActionResult"/> that renders Home and message about success or failure of operation</returns>
        public IActionResult Remove(int id)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Index","Home");

            var result = serverService.RemoveServer(id);
            if(result)
                TempData["SuccessMessage"] = "Server removed";
            else
                TempData["SuccessMessage"] = "Server doesn't exist or something went wrong. See log.";
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Saves changes to a server.
        /// </summary>
        /// <param name="server"> Model where information about server is stored for updating </param>
        /// <returns> An <see cref="IActionResult"/> that renders Home and message about success or failure of operation </returns>
        [HttpPost]
        public IActionResult SaveChanges(ServerModel server)
        {
            if(User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Index", "Home");

            var result = serverService.UpdateServer(server);
            if (result)
                TempData["SuccessMessage"] = "Server updated";
            else
                TempData["SuccessMessage"] = "Server cannot be updated. See log.";

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Tests the connection to a server.
        /// </summary>
        /// <param name="server"> Model where information about server is stored for testing connection </param>
        /// <returns>  An <see cref="Task{IActionResult}"/> that renders Add page and message about success or failure of operation </returns>
        public async Task<IActionResult> TestConnection(ServerModel server)
        {
            if(server.Password == null)
                return View("Add", server);
            if (!ModelState.IsValid)
            {
                return View("Error");
            }
            if(server.ServerType == null)
            {
                TempData["ErrorMessage"] = "Server type is not selected";
                return View("Add", server);
            }
            if (server.ServerType == "CML")
            {
                // Check if the server is reachable for CML
                var client = await authServiceCisco.ValidateCredentials(server.IpAddress, server.Username, server.Password);
                if (client.Valid)
                {
                    TempData["SuccessMessage"] = "Connection successful";
                    ViewBag.Tested = true;
                    return View("Add", server);
                }
                else
                {
                    TempData["ErrorMessage"] = "Connection failed. " + client.Message;
                    return View("Add", server);
                }
            }
            else if (server.ServerType == "EVE")
            {
                // Check if the server is reachable for EVE
                var valid = await authServiceEVE.ValidateCredentials(server.IpAddress, server.Username, server.Password);
                if (valid)
                {
                    TempData["SuccessMessage"] = "Connection successful";
                    ViewBag.Tested = true;
                    return View("Add", server);
                }
                else
                {
                    TempData["ErrorMessage"] = "Connection failed. Invalid Credentials";
                    return View("Add", server);
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Server type is not selected or invalid ";
                return View("Add", server);
            }

        }

        /// <summary>
        /// Adds a new server to the system.
        /// </summary>
        /// <param name="server">Model where information about server is stored for adding server </param>
        /// <returns> An <see  cref="Task{IActionResult}"/> that renders Home or Add page and message about success or failure of operation </returns>
        [HttpPost]
        public async Task<IActionResult> AddTestedConnection(ServerModel server)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Index", "Home");
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Add", "Server");
            }

            if (server.ServerType == "CML")
            {
                // Check if the server is reachable for CML
                var client = await authServiceCisco.ValidateCredentials(server.IpAddress, server.Username, server.Password);
                if (client.Valid)
                {
                    // Insert the server into the database
                    var ok = serverService.InsertServer(server);
                    if (ok)
                        TempData["SuccessMessage"] = "Server added successfully";
                    else
                        TempData["ErrorMessage"] = "Server cannot be added. See log.";

                    // Redirect to the home page
                    ViewBag.Servers = serverService.GetAllServers();
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["ErrorMessage"] = "Connection failed. " + client.Message;
                    return View("Add", server);
                }

            }
            else if (server.ServerType == "EVE")
            {
                // Check if the server is reachable for EVE
                var valid = await authServiceEVE.ValidateCredentials(server.IpAddress,server.Username, server.Password);
                if (valid)
                {
                    // Insert the server into the database
                    var insert = serverService.InsertServer(server);
                    if(insert)
                        TempData["SuccessMessage"] = "Server added successfully";
                    else
                        TempData["ErrorMessage"] = "Server cannot be added. See log.";

                    // Redirect to the home page
                    ViewBag.Servers = serverService.GetAllServers();
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["ErrorMessage"] = "Connection failed. Invalid Credentials";
                    return View("Add", server);
                }
            }
            else
                return RedirectToAction("Index", "Home");
        }
    }
}
