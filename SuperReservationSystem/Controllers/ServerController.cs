using ApiCisco;
using BusinessLayer.Models;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;

namespace SuperReservationSystem.Controllers
{
    public class ServerController : Controller
    {
        private ServerService serverService = new ServerService();

        public IActionResult Add()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Index", "Home");

            return View();
        }

        public IActionResult Edit(int id)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Index", "Home");

            var server = serverService.GetServerById(id);
            if (server == null)
            {
                TempData["ErrorMessage"] = "Server not found.";
                return RedirectToAction("Index", "Home");
            }
            return View(server);
        }

        public IActionResult Remove(int id)
        {
            if (!User.Identity.IsAuthenticated)
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
            if(!User.Identity.IsAuthenticated)
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
            if (!ModelState.IsValid)
            {
                return View("Error");
            }
            var client = new UserHttpClient(server.IpAddress);
            var res = await Authentication.Authenticate(client, server.Username, server.Password);
            if (res != null && res.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Connection successful";
                ViewBag.Tested = true;
                return View("AddServer", server);
            }
            else
            {
                if (res != null)
                    TempData["ErrorMessage"] = "Connection failed. " + res.Content.ReadAsStringAsync().Result;
                return View("AddServer", server);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddTestedConnection(ServerModel server)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("AddServer", "Home");
            }

            if (server.ServerType == "CML")
            {
                var client = new UserHttpClient(server.IpAddress);
                var res = await Authentication.Authenticate(client, server.Username, server.Password);
                if (res != null && res.IsSuccessStatusCode)
                {
                    var ok = serverService.InsertServer(server);
                    ViewBag.Servers = serverService.GetAllServers();
                    TempData["SuccessMessage"] = "Server added successfully";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    if (res != null)
                        TempData["ErrorMessage"] = "Connection failed. " + res.Content.ReadAsStringAsync().Result;
                    return View("AddServer", server);
                }

            }
            else
                return RedirectToAction("Index", "Home");
        }
    }
}
