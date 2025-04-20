using BusinessLayer.Interface;
using BusinessLayer.Models;
using BusinessLayer.Services;
using BusinessLayer.Services.ApiCiscoServices;
using BusinessLayer.Services.ApiEVEServices;
using Microsoft.AspNetCore.Mvc;
using SuperReservationSystem.Models;

namespace SuperReservationSystem.Controllers
{
    /// <summary>
    /// Controller for managing user-related actions.
    /// </summary>
    public class UserController : Controller
    {
        private readonly UserLabOwnershipService userLabOwnershipService = new UserLabOwnershipService();
        private readonly ApiCiscoLabService labServiceCisco = new ApiCiscoLabService();
        private readonly ApiEVELabService labServiceEVE = new ApiEVELabService();
        private readonly UserService userService = new UserService();
        private readonly ServerService serverService = new ServerService();

        /// <summary>
        /// Displays the settings page for the user.
        /// </summary>
        /// <returns> An <see cref="IActionResult"/> that renders the user settings view. </returns>
        public IActionResult Settings()
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");

            ViewBag.UserAuthType = userService.GetAuthorizationType(User.Identity?.Name ?? string.Empty);

            return View();
        }

        /// <summary>
        /// Displays the user lab ownership page.
        /// </summary>
        /// <returns>  A <see cref="Task{IActionResult}"/> that renders the view showing labs owned or assigned to the current user.  </returns>
        public async Task<IActionResult> UserLab()
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");

            // Get the user ID from the UserService
            var userId = userService.GetUserId(User.Identity?.Name ?? string.Empty);

            if (userId == 0)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index", "Home");
            }
            // Get all labs owned by the user
            var allUserLabs = userLabOwnershipService.GetAllUserLabsByUserID(userId);

            if (allUserLabs != null)
            {
                var labsInfo = new List<(int, ILabModel)>(); // Tuple of server id and lab model
                // Loop through each owned lab and get its information
                foreach (var owned in allUserLabs)
                {
                    var server = serverService.ServerExists(owned.ServerId);
                    var serverType = serverService.GetServerType(owned.ServerId);
                    if (server && serverType=="CML")
                    {                        
                        var lab = await labServiceCisco.GetLabInfo(owned.ServerId, owned.LabId);
                        if (lab.lab != null)
                            labsInfo.Add((owned.ServerId, lab.lab));
                    }
                    else if(server && serverType=="EVE")
                    {
                        var lab = await labServiceEVE.GetLabInfoById(owned.ServerId, owned.LabId);
                        if (lab != null)
                            labsInfo.Add((owned.ServerId, lab));
                    }
                }
                ViewBag.Labs = labsInfo;
            }

            return View();
        }

        /// <summary>
        /// Handles the ownership of a lab by a user.
        /// </summary>
        /// <param name="serverID"> ID of server where lab is </param>
        /// <param name="labID"> Lab ID user wants to own</param>
        /// <returns> An <see cref="IActionResult"/> that redirects to different page </returns>
        public IActionResult OwnLab(int serverID, string labID)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Access denied. Log in to use this feature.";
                return RedirectToAction("Login", "Home");
            }
            var server = serverService.ServerExists(serverID);
            if (!server)
            {
                TempData["ErrorMessage"] = "Server not found.";
                return RedirectToAction("Index", "Home");
            }
            var serverType = serverService.GetServerType(serverID);
            UserLabOwnershipModel model = new UserLabOwnershipModel
            {
                ServerId = serverID,
                LabId = labID,
                UserId = userService.GetUserId(User.Identity?.Name ?? string.Empty)
            };
            var lab = userLabOwnershipService.InsertUserLabOwnership(model);
            if (lab.Item1)
            {
                TempData["SuccessMessage"] = "Lab owned successfully.";
                return RedirectToAction("LabInfo", $"{serverType}", new { id = serverID, labId = labID });
            }
            TempData["ErrorMessage"] = lab.Item2;
            return RedirectToAction("LabList", $"{serverType}", new { id = serverID });
        }

        /// <summary>
        /// Removes ownership of a lab from a user.
        /// </summary>
        /// <param name="serverId"> Server ID where a lab is</param>
        /// <param name="labId"> Lab ID which user doesn't want to own anymore</param>
        /// <returns> An <see cref="IActionResult"/> that redirects to different page </returns>
        public IActionResult RemoveOwnership(int serverId, string labId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Access denied. Log in to use this feature.";
                return RedirectToAction("Login", "Home");
            }
            var userId = userService.GetUserId(User.Identity?.Name ?? string.Empty);
            UserLabOwnershipModel model = new UserLabOwnershipModel
            {
                ServerId = serverId,
                LabId = labId,
                UserId = userId
            };
            var lab = userLabOwnershipService.DeleteUserLabOwnership(model);
            if (lab)
            {
                TempData["SuccessMessage"] = "Lab ownership removed successfully.";
                return RedirectToAction("UserLab", "User");
            }
            TempData["ErrorMessage"] = "Lab ownership could not be removed.";
            return RedirectToAction("UserLab", "User");
        }

        /// <summary>
        /// Changes the password of the user.
        /// </summary>
        /// <param name="PassModel"> Model where old and new password is forwarded to </param>
        /// <returns>  An <see cref="IActionResult"/> that renders user settings and message if it was successful or not </returns>
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

        /// <summary>
        /// Adds a new user to the system.
        /// </summary>
        /// <param name="model"> Model where new user is stored </param>
        /// <returns>  An <see cref="IActionResult"/> that renders user settings and message if it was successful or not </returns>
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

        /// <summary>
        /// Displays the user management page.
        /// </summary>
        /// <returns>  An <see cref="IActionResult"/> that renders ManageUser page </returns>
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

        /// <summary>
        /// Deactivates a user in the system.
        /// </summary>
        /// <param name="UserId"> ID of user to deactivate </param>
        /// <returns>  An <see cref="IActionResult"/> that renders ManageUser and if it was successful or not </returns>
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

        /// <summary>
        /// Activates a user in the system.
        /// </summary>
        /// <param name="UserId"> ID of user to activate </param>
        ///  <returns>  An <see cref="IActionResult"/> that renders ManageUser and if it was successful or not </returns>
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

        /// <summary>
        /// Removes a user from the system.
        /// </summary>
        /// <param name="UserId"> ID of user to activate </param>
        /// <returns>  An <see cref="IActionResult"/> that renders ManageUser and if it was successful or not </returns>
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
