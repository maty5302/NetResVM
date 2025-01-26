using ApiCisco;
using ApiEVE;
using BusinessLayer.MapperDT;
using BusinessLayer.Models;
using BusinessLayer.Services;
using BusinessLayer.Services.ApiCiscoServices;
using BusinessLayer.Services.ApiEVEServices;
using Microsoft.AspNetCore.Mvc;

namespace SuperReservationSystem.Controllers
{
    public class ServerController : Controller
    {
        private ServerService serverService = new ServerService();
        private ApiCiscoAuthService authServiceCisco = new ApiCiscoAuthService();
        private ApiEVEAuthService authServiceEVE = new ApiEVEAuthService();

        public IActionResult Add()
        {
            if (User.Identity!=null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Index", "Home");

            return View();
        }

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

        public async Task<IActionResult> TestConnection(ServerModel server)
        {
            if(server.Password == null)
                return View("Add", server);
            if (!ModelState.IsValid)
            {
                return View("Error");
            }
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

        [HttpPost]
        public async Task<IActionResult> AddTestedConnection(ServerModel server)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Add", "Server");
            }

            if (server.ServerType == "CML")
            {
                var client = await authServiceCisco.ValidateCredentials(server.IpAddress, server.Username, server.Password);
                if (client.Valid)
                {
                    var ok = serverService.InsertServer(server);
                    if (ok)
                        TempData["SuccessMessage"] = "Server added successfully";
                    else
                        TempData["ErrorMessage"] = "Server cannot be added. See log.";
                    
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
                var valid = await authServiceEVE.ValidateCredentials(server.IpAddress,server.Username, server.Password);
                if (valid)
                {
                    var insert = serverService.InsertServer(server);
                    if(insert)
                        TempData["SuccessMessage"] = "Server added successfully";
                    else
                        TempData["ErrorMessage"] = "Server cannot be added. See log.";

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
