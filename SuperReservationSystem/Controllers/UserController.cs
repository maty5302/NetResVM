using ApiCisco;
using ApiCisco.Model;
using BusinessLayer.Models;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;

namespace SuperReservationSystem.Controllers
{
    public class UserController : Controller
    {
        private UserLabOwnershipService userLabOwnershipService = new UserLabOwnershipService();
        private UserService userService = new UserService();
        private ServerService serverService = new ServerService();

        public IActionResult Settings()
        {
            if(User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            return View();
        }

        public async Task<IActionResult> UserLab()
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");

            var userId = userService.GetUserId(User.Identity?.Name ?? string.Empty);
            var allUserLabs = userLabOwnershipService.GetAllUserLabsByUserID(userId);

            if (allUserLabs != null)
            {
                var labsInfo = new List<(int, LabModel)>(); // Tuple of server id and lab model
                foreach (var item in allUserLabs)
                {
                    var server = serverService.GetServerById(item.ServerId);
                    if (server != null)
                    {
                        var client = new UserHttpClient(server.IpAddress);
                        var auth = await Authentication.Authenticate(client, server.Username, server.Password);
                        var lab = await Lab.GetLabInfo(client, item.LabId);
                        if (lab != null)
                            labsInfo.Add((server.Id, lab));
                    }
                }
                ViewBag.Labs = labsInfo;
            }

            return View();
        }


        public IActionResult OwnLab(int serverID, string labID)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Access denied. Log in to use this feature.";
                return RedirectToAction("Login", "Home");
            }
            var server = serverService.GetServerById(serverID);
            if (server == null)
            {
                TempData["ErrorMessage"] = "Server not found.";
                return RedirectToAction("Index", "Home");
            }
            UserLabOwnershipModel model = new UserLabOwnershipModel
            {
                ServerId = serverID,
                LabId = labID,
                UserId = userService.GetUserId(User.Identity.Name)
            };
            var lab = userLabOwnershipService.InsertUserLabOwnership(model);
            if (lab.Item1)
            {
                TempData["SuccessMessage"] = "Lab owned successfully.";
                return RedirectToAction("LabInfo", "CML", new { id = serverID, id_lab=labID });
            }
            TempData["ErrorMessage"] = lab.Item2;
            return RedirectToAction("LabList", "CML", new { id = serverID });
        }
    }
}
