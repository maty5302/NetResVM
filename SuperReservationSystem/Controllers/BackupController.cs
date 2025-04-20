using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;

namespace SuperReservationSystem.Controllers;

/// <summary>
/// Controller for managing backups.
/// </summary>
public class BackupController : Controller
{
    private readonly BackupService _backupService = new BackupService();
    private readonly UserLabOwnershipService _labOwnershipService = new UserLabOwnershipService();
    private readonly UserService _userService = new UserService();

    /// <summary>
    /// Displays the list of backups.
    /// </summary>
    /// <returns> An <see cref="IActionResult"/> that renders main backup page</returns>
    public async Task<IActionResult> Index()
    {
        if (User.Identity != null && !User.Identity.IsAuthenticated)
        {
            TempData["ErrorMessage"] = "Access denied. Log in to use this feature.";
            return RedirectToAction("Login", "Home");
        }
        var allBackups = await _backupService.GetBackups();
        
        ViewBag.Backups = allBackups;
        return View();
    }

    /// <summary>
    /// Creates a backup of a lab for a specified server.
    /// </summary>
    /// <param name="serverId"> ID of the server </param>
    /// <param name="labId"> ID of lab to be made backup for</param>
    /// <param name="serverType"> Type of the server </param>
    /// <param name="fromServer"> If it was made from LabInfo page or not </param>
    /// <returns>  An <see cref="IActionResult"/> that renders main backup page or labinfo page and message about success or failure of operation </returns>
    public async Task<IActionResult> MakeBackup(int serverId, string labId, string serverType, bool fromServer=false)
    {
        if (User.Identity != null && !User.Identity.IsAuthenticated)
            return RedirectToAction("Index", "Login");
        var result = await _backupService.BackupLab(serverId, labId, serverType);
        if (result.backup)
        {
            TempData["SuccessMessage"] = "Backup was successful.";
        }
        else
        {
            TempData["ErrorMessage"] = "Backup failed.";
        }

        if (fromServer)
            return RedirectToAction("LabInfo", $"{serverType}", new{id=serverId, labId=labId});
        return RedirectToAction("Index", "Backup");
    }

    /// <summary>
    /// Restores a backup for a specified server.
    /// </summary>
    /// <param name="serverId"> ID of the server </param>
    /// <param name="labId"> ID of lab to restore backup for</param>
    /// <param name="serverType"> Type of the server </param>
    /// <param name="filename"> Name of the file </param>
    /// <returns>  An <see cref="IActionResult"/> that renders main backup page and message about success or failure of operation</returns>
    public async Task<IActionResult> RestoreBackup(int serverId, string labId, string serverType, string filename)
    {
        if (User.Identity != null && !User.Identity.IsAuthenticated)
            return RedirectToAction("Index", "Login");
        var owned = _labOwnershipService.IsLabAlreadyOwned(_userService.GetUserId(User.Identity.Name), labId);
        if(!owned.owned)
        {
            TempData["ErrorMessage"] = "Cannot restore lab backup... You do not own this lab.";
            return RedirectToAction("Index", "Backup");
        }
        var result = await _backupService.RestoreBackup(serverId,serverType, labId, filename);
        if (result)
        {
            TempData["SuccessMessage"] = "Restore was successful.";
        }
        else
        {
            TempData["ErrorMessage"] = "Restore failed.";
        }
        return RedirectToAction("Index", "Backup");
    }

    /// <summary>
    /// Deletes a backup for a specified server.
    /// </summary>
    /// <param name="filename"> Name of the file to delete </param>
    /// <param name="labId"> ID of lab to delete backup for</param>
    /// <param name="serverType"> Type of the server </param>
    /// <returns>  An <see cref="IActionResult"/> that renders main backup page and message about success or failure of operation</returns>
    public IActionResult DeleteBackup(string filename, string labId, string serverType)
    {
        if (User.Identity != null && !User.Identity.IsAuthenticated)
            return RedirectToAction("Index", "Login");
        var owned = _labOwnershipService.IsLabAlreadyOwned(_userService.GetUserId(User.Identity.Name), labId);
        if(!owned.owned)
        {
            TempData["ErrorMessage"] = "Cannot delete lab backup... You do not own this lab.";
            return RedirectToAction("Index", "Backup");
        }
        var result = _backupService.DeleteBackup(filename, labId, serverType);
        if (result)
        {
            TempData["SuccessMessage"] = "Backup was deleted.";
        }
        else
        {
            TempData["ErrorMessage"] = "Delete failed.";
        }
        return RedirectToAction("Index", "Backup");
    }

    /// <summary>
    /// Downloads a backup file.
    /// </summary>
    /// <param name="filename"> Name of the file to download </param>
    /// <param name="labId"> ID of lab to download backup for</param>
    /// <param name="serverType"> Type of the server </param>
    /// <returns>  An <see cref="IActionResult"/> that renders main backup page and message about success or failure of operation</returns>
    public async Task<IActionResult> DownloadBackup(string filename, string labId, string serverType)
    {
        if (User.Identity != null && !User.Identity.IsAuthenticated)
            return RedirectToAction("Index", "Login");
        var file = await _backupService.DownloadBackup(serverType, labId, filename);
        if (file.Length > 0)
        {
            return File(file, "application/octet-stream", filename);
        }
        TempData["ErrorMessage"] = "Download failed.";
        return RedirectToAction("Index", "Backup");
    }
}