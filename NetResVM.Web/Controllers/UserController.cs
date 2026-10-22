using BusinessLayer.DTOs;
using BusinessLayer.Enum;
using BusinessLayer.Interface;
using BusinessLayer.Models;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NetResVM.Web.Models;

namespace NetResVM.Web.Controllers
{
    /// <summary>
    /// Controller for managing user-related actions.
    /// </summary>
    public class UserController : Controller
    {
        private readonly UserLabOwnershipService _userLabOwnershipService;
        private readonly IPlatformManager _platformManager;
        private readonly UserService _userService;
        private readonly ServerService _serverService;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly UserImportService _userImportService;

        public UserController(
            IPlatformManager platformManager,
            UserLabOwnershipService userLabOwnershipService,
            UserService userService,
            ServerService serverService,
            IStringLocalizer<SharedResource> localizer,
            UserImportService? userImportService = null)
        {
            _platformManager = platformManager;
            _userLabOwnershipService = userLabOwnershipService;
            _userService = userService;
            _serverService = serverService;
            _localizer = localizer;
            _userImportService = userImportService ?? new UserImportService(userService);
        }
        /// <summary>
        /// Displays the settings page for the user.
        /// </summary>
        /// <returns> An <see cref="IActionResult"/> that renders the user settings view. </returns>
        public IActionResult Settings()
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Login");

            ViewBag.UserAuthType = _userService.GetAuthorizationType(User.Identity?.Name ?? string.Empty);

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
            var userId = _userService.GetUserId(User.Identity?.Name ?? string.Empty);

            if (userId == 0)
            {
                TempData["ErrorMessage"] = _localizer["UserNotFound"].Value;
                return RedirectToAction("Index", "Home");
            }
            // Get all labs owned by the user
            var allUserLabs = _userLabOwnershipService.GetAllUserLabsByUserID(userId);
            var labsInfo = new List<(int, LabDTO)>();// Tuple of server id and lab model
            if (allUserLabs != null)
            {
               
                // Loop through each owned lab and get its information
                foreach (var owned in allUserLabs)
                {
                    var server = _serverService.ServerExists(owned.ServerId);
                    var platform = _serverService.GetServerType(owned.ServerId);
                    var online = _serverService.IsServerOnlineAsync(_serverService.GetServerById(owned.ServerId).IpAddress ?? string.Empty).Result;
                    
                    if(!online)
                    {
                        continue; // Skip if server is offline
                    } 
                    if (platform == PlatformType.Unknown)
                    {
                        continue; // Skip if platform type is unknown
                    }

                    IVirtualizationAdapter adapter = _platformManager.GetAdapter(platform);
                    if(server)
                    {
                        var lab = await adapter.GetLabInfoAsync(owned.ServerId, owned.LabId);
                        if (lab.Lab != null)
                            labsInfo.Add((owned.ServerId, lab.Lab));
                    }
                }
            }

            return View(labsInfo);
        }

        /// <summary>
        /// Handles the ownership of a lab by a user.
        /// </summary>
        /// <param name="serverID"> ID of server where lab is </param>
        /// <param name="labID"> Lab ID user wants to own</param>
        /// <returns> An <see cref="IActionResult"/> that redirects to different page </returns>
        [HttpPost]
        public IActionResult OwnLab(int serverID, string labID)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = _localizer["AccessDenied"].Value;
                return RedirectToAction("Index", "Login");
            }
            var server = _serverService.ServerExists(serverID);
            if (!server)
            {
                TempData["ErrorMessage"] = _localizer["ServerNotFound"].Value;
                return RedirectToAction("Index", "Home");
            }
            var currentUserId = _userService.GetUserId(User.Identity?.Name ?? string.Empty);
            // SECURITY: a lab that already has an owner can only be joined via AddCollaborator (main owner/admin)
            var ownership = _userLabOwnershipService.IsLabAlreadyOwned(currentUserId, serverID, labID);
            if (ownership.owned)
            {
                TempData["ErrorMessage"] = _localizer["AccessDenied"].Value;
                return RedirectToAction("LabInfo", "Platform", new { serverId = serverID, labId = labID });
            }
            UserLabOwnershipModel model = new UserLabOwnershipModel
            {
                ServerId = serverID,
                LabId = labID,
                UserId = currentUserId
            };
            var lab = _userLabOwnershipService.InsertUserLabOwnership(model);
            if (lab.Item1)
            {
                TempData["SuccessMessage"] = _localizer["LabOwnedSuccess"].Value;
                return RedirectToAction("LabInfo", "Platform", new { serverId = serverID, labId = labID });
            }
            TempData["ErrorMessage"] = lab.Item2;
            return RedirectToAction("LabList", "Platform", new { serverId = serverID });
        }

        /// <summary>
        /// Removes ownership of a lab from a user.
        /// </summary>
        /// <param name="serverId">The unique identifier of the server where the lab is hosted.</param>
        /// <param name="labId">The unique identifier of the lab which the user doesn't want to own anymore.</param>
        /// <returns>An <see cref="IActionResult"/> that redirects to a different page.</returns>
        [HttpPost]
        public IActionResult RemoveOwnership(int serverId, string labId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = _localizer["AccessDenied"].Value;
                return RedirectToAction("Index", "Login"); 
            }
            var userId = _userService.GetUserId(User.Identity?.Name ?? string.Empty);
            UserLabOwnershipModel model = new UserLabOwnershipModel
            {
                ServerId = serverId,
                LabId = labId,
                UserId = userId
            };
            var lab = _userLabOwnershipService.DeleteUserLabOwnership(model);
            if (lab)
            {
                TempData["SuccessMessage"] = _localizer["LabOwnedRemoved"].Value;
                return RedirectToAction("UserLab", "User");
            }
            TempData["ErrorMessage"] = _localizer["LabOwnedRemoveError"].Value;
            return RedirectToAction("UserLab", "User");
        }

        /// <summary>
        ///  Removes ownership of a lab from a user.
        /// </summary>
        /// <param name="serverId">The unique identifier of the server where the lab is hosted.</param>
        /// <param name="labId">The unique identifier of the lab which the user doesn't want to own anymore.</param>
        /// <param name="username">The username of the user whose ownership of the lab is to be removed.</param>
        /// <returns>An <see cref="IActionResult"/> that redirects to the appropriate page based on the outcome of the operation.</returns>
        [HttpPost]
        public IActionResult RemoveOwnershipWithUsername(int serverId, string labId, string username)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = _localizer["AccessDenied"].Value;
                return RedirectToAction("Index", "Login");
            }
            var userId = _userService.GetUserId(username);
            var currentUserId = _userService.GetUserId(User.Identity?.Name ?? string.Empty);
            // SECURITY: only admin, the main owner, or the user himself can remove an ownership
            bool allowed = User.IsInRole("Admin")
                           || (userId > 0 && userId == currentUserId)
                           || _userLabOwnershipService.IsMainOwner(currentUserId, serverId, labId);
            if (userId <= 0 || !allowed)
            {
                TempData["ErrorMessage"] = _localizer["AccessDenied"].Value;
                return RedirectToAction("LabInfo", "Platform", new { serverId, labId });
            }
            UserLabOwnershipModel model = new UserLabOwnershipModel
            {
                ServerId = serverId,
                LabId = labId,
                UserId = userId
            };
            var lab = _userLabOwnershipService.DeleteUserLabOwnership(model);
            if (lab)
            {
                TempData["SuccessMessage"] = _localizer["LabOwnedRemoved"].Value;
                return RedirectToAction("LabInfo", "Platform", new {serverId,labId});
            }
            TempData["ErrorMessage"] = _localizer["LabOwnedRemoveError"].Value;
            return RedirectToAction("LabInfo", "Platform", new {serverId,labId});
        }

        /// <summary>
        /// Changes the password of the user.
        /// </summary>
        /// <param name="PassModel"> Model where old and new password is forwarded to </param>
        /// <returns>  An <see cref="IActionResult"/> that renders user settings and message if it was successful or not </returns>
        [HttpPost]
        public IActionResult ChangePassword(ChangePasswordModel PassModel)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = _localizer["AccessDenied"].Value;
                return RedirectToAction("Index", "Login"); 
            }
            if (!_userService.ValidateCredentials(User.Identity.Name, PassModel.oldPassword))
            {
                TempData["ErrorMessage"] = _localizer["OldPassIcorrect"].Value;
                return RedirectToAction("Settings", "User");
            }
            if (PassModel.newPassword != PassModel.confirmPassword)
            {
                TempData["ErrorMessage"] = _localizer["PasswordNotMatch"].Value;
                return RedirectToAction("Settings", "User");
            }
            if (_userService.UpdateUser(_userService.GetUserId(User.Identity.Name), PassModel.newPassword))
            {
                TempData["SuccessMessage"] = _localizer["PasswordChanged"].Value;
                return RedirectToAction("Settings", "User");
            }
            TempData["ErrorMessage"] = _localizer["PasswordError"].Value;
            return RedirectToAction("Settings", "User");
        }

        /// <summary>
        /// Adds a new user to the system.
        /// </summary>
        /// <param name="model"> Model where new user is stored </param>
        /// <returns>  An <see cref="IActionResult"/> that renders user settings and message if it was successful or not </returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult AddUser(UserModel model)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = _localizer["AccessDenied"].Value;
                return RedirectToAction("Index", "Login");
            }
            if(model.AuthorizationType=="localhost" && model.Password==null)
            {
                TempData["ErrorMessage"] = _localizer["PasswordRequired"].Value;
                return RedirectToAction("Settings", "User");
            }
            string role = string.IsNullOrWhiteSpace(model.Role) ? "student" : model.Role;
            if (_userService.AddUser(model.Username, model.Password, role, model.AuthorizationType, model.Active))
            {
                TempData["SuccessMessage"] = _localizer["UserAddedSuccess"].Value;
                return RedirectToAction("Settings", "User");
            }
            TempData["ErrorMessage"] = _localizer["UserAddedFail"].Value;
            return RedirectToAction("Settings", "User");
        }

        /// <summary>
        /// Imports users in bulk from a CSV or TXT file.
        /// </summary>
        /// <param name="file">The uploaded file (.csv or .txt).</param>
        /// <param name="authType">Authentication type (vsb or localhost).</param>
        /// <param name="role">Role assigned to imported users (default: student).</param>
        /// <returns>Redirects back to Settings with a status message.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult ImportUsers(IFormFile? file, string authType = "vsb", string role = "student")
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = _localizer["AccessDenied"].Value;
                return RedirectToAction("Index", "Login");
            }
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = _localizer["AccessDeniedAdmin"].Value;
                return RedirectToAction("Index", "Home");
            }

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = _localizer["FileEmptyOrMissing"].Value;
                return RedirectToAction("Settings", "User");
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".csv" && extension != ".txt")
            {
                TempData["ErrorMessage"] = _localizer["UnsupportedFileFormat"].Value;
                return RedirectToAction("Settings", "User");
            }

            if (authType != "vsb" && authType != "localhost")
            {
                TempData["ErrorMessage"] = _localizer["ImportInvalidAuthType"].Value;
                return RedirectToAction("Settings", "User");
            }

            try
            {
                using var stream = file.OpenReadStream();
                var result = _userImportService.ImportUsersFromStream(stream, file.FileName, role, authType, active: true);

                if (result.MissingPasswordUsernames.Count > 0)
                {
                    // localhost import: nothing was imported because some users have no password
                    string preview = string.Join(", ", result.MissingPasswordUsernames.Take(5));
                    if (result.MissingPasswordUsernames.Count > 5)
                    {
                        preview += ", ...";
                    }
                    TempData["ErrorMessage"] = string.Format(_localizer["ImportMissingPasswords"].Value, result.MissingPasswordUsernames.Count, preview);
                }
                else if (result.TotalFound == 0)
                {
                    TempData["ErrorMessage"] = _localizer["ImportNoUsersFound"].Value;
                }
                else if (result.AddedCount == 0 && result.InvalidCount == 0 && result.SkippedCount > 0)
                {
                    TempData["ErrorMessage"] = string.Format(_localizer["ImportAllSkippedMessage"].Value, result.SkippedCount);
                }
                else if (result.AddedCount == 0)
                {
                    TempData["ErrorMessage"] = string.Format(_localizer["ImportFailedMessage"].Value, result.InvalidCount);
                }
                else
                {
                    string msg = string.Format(_localizer["ImportSuccessMessage"].Value, result.AddedCount);
                    if (result.SkippedCount > 0)
                    {
                        msg += string.Format(_localizer["ImportSkippedInfo"].Value, result.SkippedCount);
                    }
                    TempData["SuccessMessage"] = msg;
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = _localizer["AnErrorOccured"].Value;
            }

            return RedirectToAction("Settings", "User");
        }

        /// <summary>
        /// Displays the user management page.
        /// </summary>
        /// <returns>  An <see cref="IActionResult"/> that renders ManageUser page </returns>
        [Authorize(Roles = "Admin")]
        public IActionResult ManageUser()
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = _localizer["AccessDenied"].Value;
                return RedirectToAction("Index", "Login");
            }
            if(!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = _localizer["AccessDeniedAdmin"].Value;
                return RedirectToAction("Index", "Home");
            }
            var users = _userService.GetAllUsersInfo();
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
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult DeactivateUser(int UserId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = _localizer["AccessDenied"].Value;
                return RedirectToAction("Index", "Login");
            }
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = _localizer["AccessDeniedAdmin"].Value;
                return RedirectToAction("Index", "Home");
            }
            if (_userService.UpdateUser(UserId,false))
            {
                TempData["SuccessMessage"] = _localizer["UserDeactivated"].Value;
                return RedirectToAction("ManageUser", "User");
            }
            TempData["ErrorMessage"] = _localizer["UserDeactivatedFail"].Value;
            return RedirectToAction("ManageUser", "User");
        }

        /// <summary>
        /// Activates a user in the system.
        /// </summary>
        /// <param name="UserId"> ID of user to activate </param>
        ///  <returns>  An <see cref="IActionResult"/> that renders ManageUser and if it was successful or not </returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult ActivateUser(int UserId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = _localizer["AccessDenied"].Value;
                return RedirectToAction("Index", "Login");
            }
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = _localizer["AccessDeniedAdmin"].Value;
                return RedirectToAction("Index", "Home");
            }
            if (_userService.UpdateUser(UserId, true))
            {
                TempData["SuccessMessage"] = _localizer["UserActivated"].Value;
                return RedirectToAction("ManageUser", "User");
            }
            TempData["ErrorMessage"] = _localizer["UserActivatedFail"].Value;
            return RedirectToAction("ManageUser", "User");
        }

        /// <summary>
        /// Removes a user from the system.
        /// </summary>
        /// <param name="UserId"> ID of user to activate </param>
        /// <returns>  An <see cref="IActionResult"/> that renders ManageUser and if it was successful or not </returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult RemoveUser(int UserId)
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = _localizer["AccessDenied"].Value;
                return RedirectToAction("Index", "Login");
            }
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = _localizer["AccessDeniedAdmin"].Value;
                return RedirectToAction("Index", "Home");
            }
            if (_userService.RemoveUser(UserId))
            {
                TempData["SuccessMessage"] = _localizer["UserRemoved"].Value;
                return RedirectToAction("ManageUser", "User");
            }
            TempData["ErrorMessage"] =  _localizer["UserRemovedFail"].Value;
            return RedirectToAction("ManageUser", "User");
        }
    }
}
