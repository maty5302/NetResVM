using ApiCisco;
using ApiCisco.Model;
using BusinessLayer.Models;
using BusinessLayer.Services;
using BusinessLayer.Services.ApiCiscoServices;
using Microsoft.AspNetCore.Mvc;
using SuperReservationSystem.Models;

namespace SuperReservationSystem.Controllers
{
    public class UserController : Controller
    {
        private UserLabOwnershipService userLabOwnershipService = new UserLabOwnershipService();
        private ApiCiscoAuthService authService = new ApiCiscoAuthService();
        private UserService userService = new UserService();
        private ServerService serverService = new ServerService();

        public IActionResult Settings()
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");
            
            ViewBag.UserAuthType = userService.GetAuthorizationType(User.Identity.Name);
            
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
                        var auth = await authService.AuthenticateAndCreateClient(item.ServerId);
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
                return RedirectToAction("LabInfo", "CML", new { id = serverID, id_lab = labID });
            }
            TempData["ErrorMessage"] = lab.Item2;
            return RedirectToAction("LabList", "CML", new { id = serverID });
        }

        public IActionResult ChangePassword(ChangePasswordModel PassModel)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Access denied. Log in to use this feature.";
                return RedirectToAction("Login", "Home");
            }
            if (!userService.ValidateCredentials(User.Identity.Name, PassModel.oldPassword))
            {
                TempData["ErrorMessage"] = "Old password is incorrect.";
                return RedirectToAction("Settings", "User");
            }
            if (PassModel.newPassword != PassModel.confirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                return RedirectToAction("Settings", "User");
            }
            if (userService.UpdateUser(userService.GetUserId(User.Identity.Name), PassModel.newPassword))
            {
                TempData["SuccessMessage"] = "Password changed successfully.";
                return RedirectToAction("Settings", "User");
            }
            TempData["ErrorMessage"] = "Password could not be changed.";
            return RedirectToAction("Settings", "User");
        }

        public IActionResult AddUser(UserModel model)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Access denied. Log in to use this feature.";
                return RedirectToAction("Login", "Home");
            }
            if(model.AuthorizationType=="localhost" && model.Password==null)
            {
                TempData["ErrorMessage"] = "Password is required for local account.";
                return RedirectToAction("Settings", "User");
            }
            if (userService.AddUser(model.Username, model.Password, "student", model.AuthorizationType, model.Active))
            {
                TempData["SuccessMessage"] = "User added successfully.";
                return RedirectToAction("Settings", "User");
            }
            TempData["ErrorMessage"] = "User could not be added.";
            return RedirectToAction("Settings", "User");
        }

        public IActionResult ManageUser()
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Access denied. Log in to use this feature.";
                return RedirectToAction("Login", "Home");
            }
            if(!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Access denied. Admin role required.";
                return RedirectToAction("Index", "Home");
            }
            var users = userService.GetAllUsersInfo();
            if (users != null)
            {
                ViewBag.Users = users;
            }
            return View();
        }

        public IActionResult DeactivateUser(int UserId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Access denied. Log in to use this feature.";
                return RedirectToAction("Login", "Home");
            }
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Access denied. Admin role required.";
                return RedirectToAction("Index", "Home");
            }
            if (userService.UpdateUser(UserId,false))
            {
                TempData["SuccessMessage"] = "User deactivated successfully.";
                return RedirectToAction("ManageUser", "User");
            }
            TempData["ErrorMessage"] = "User could not be deactivated.";
            return RedirectToAction("ManageUser", "User");
        }

        public IActionResult ActivateUser(int UserId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Access denied. Log in to use this feature.";
                return RedirectToAction("Login", "Home");
            }
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Access denied. Admin role required.";
                return RedirectToAction("Index", "Home");
            }
            if (userService.UpdateUser(UserId, true))
            {
                TempData["SuccessMessage"] = "User activated successfully.";
                return RedirectToAction("ManageUser", "User");
            }
            TempData["ErrorMessage"] = "User could not be deactivated.";
            return RedirectToAction("ManageUser", "User");
        }

        public IActionResult RemoveUser(int UserId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Access denied. Log in to use this feature.";
                return RedirectToAction("Login", "Home");
            }
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Access denied. Admin role required.";
                return RedirectToAction("Index", "Home");
            }
            if (userService.RemoveUser(UserId))
            {
                TempData["SuccessMessage"] = "User removed successfully.";
                return RedirectToAction("ManageUser", "User");
            }
            TempData["ErrorMessage"] = "User could not be removed. See log..";
            return RedirectToAction("ManageUser", "User");
        }
    }
}
