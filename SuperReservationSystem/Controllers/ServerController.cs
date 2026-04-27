using BusinessLayer.Enum;
using BusinessLayer.Interface;
using BusinessLayer.MapperDT;
using BusinessLayer.Models;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;

namespace NetResVM.Controllers
{
    /// <summary>
    /// Controller for managing servers.
    /// </summary>
    public class ServerController : Controller
    {
        private readonly ServerService _serverService;
        private readonly PlatformManager _platformManager;

        public ServerController(PlatformManager platformManager, ServerService serverService)
        {
            _platformManager = platformManager;
            _serverService = serverService;
        }
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

            var server = _serverService.GetServerById(id);
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

            var result = _serverService.RemoveServer(id);
            if(result)
                TempData["SuccessMessage"] = "Server removed";
            else
                TempData["ErrorMessage"] = "Server doesn't exist or something went wrong. See log.";
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

            var result = _serverService.UpdateServer(server);
            if (result)
                TempData["SuccessMessage"] = "Server updated";
            else
                TempData["ErrorMessage"] = "Server cannot be updated. See log.";

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Tests the connection to a server.
        /// </summary>
        /// <param name="server"> Model where information about server is stored for testing connection </param>
        /// <returns>  An <see cref="Task{IActionResult}"/> that renders Add page and message about success or failure of operation </returns>
        public async Task<IActionResult> TestConnection(ServerModel server, bool edit = false)
        {
            if (server.Password == null)
            {
                TempData["ErrorMessage"] = "You need a password for testing connection.";
                if (edit)
                    return RedirectToAction("Edit", "Server", new { id = server.Id });
                return View("Add", server);
            }
            if (!ModelState.IsValid)
            {
                return View("Error");
            }
            if (server.Platform == PlatformType.Unknown)
            {
                TempData["ErrorMessage"] = "Platform type is not selected or not supported";
                return View("Add", server);
            }
            IVirtualizationAdapter adapter = _platformManager.GetAdapter(server.Platform);
            var authResult = await adapter.TestConnection(server.IpAddress, server.Username, server.Password);
            if (authResult.Valid)
            {
                TempData["SuccessMessage"] = "Connection successful";
                ViewBag.Tested = true;

                if (edit)
                    return RedirectToAction("Edit", "Server", new { id = server.Id });
                return View("Add", server);
            }
            TempData["ErrorMessage"] = "Connection failed. " + authResult.Message;

            if (edit)
               return RedirectToAction("Edit", "Server", new { id = server.Id }); 
            return View("Add", server);
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
            IVirtualizationAdapter adapter = _platformManager.GetAdapter(server.Platform);
            
            var authResult = await adapter.TestConnection(server.IpAddress, server.Username, server.Password);
            if (authResult.Valid)
            {
                var ok = _serverService.InsertServer(server);
                if (ok)
                    TempData["SuccessMessage"] = "Server added successfully";
                else
                    TempData["ErrorMessage"] = "Server cannot be added. See log.";
                
                ViewBag.Servers = _serverService.GetAllServers();
                return RedirectToAction("Index", "Home");
            }
            
            TempData["ErrorMessage"] = "Connection failed. " + authResult.Message;
            return View("Add", server);
        }
    }
}
