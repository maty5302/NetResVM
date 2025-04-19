using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;

namespace SuperReservationSystem.Controllers;

public class BackupController : Controller
{
    private readonly BackupService _backupService = new BackupService();
    private readonly UserLabOwnershipService _labOwnershipService = new UserLabOwnershipService();
    private readonly UserService _userService = new UserService();
    // GET
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
    
    public async Task<IActionResult> MakeBackup(int serverId, string labId, string serverType, bool fromServer=false)
    {
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
    
    public async Task<IActionResult> RestoreBackup(int serverId, string labId, string serverType, string filename)
    {
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
    
    public IActionResult DeleteBackup(string filename, string labId, string serverType)
    {
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
    
    public async Task<IActionResult> DownloadBackup(string filename, string labId, string serverType)
    {
        var file = await _backupService.DownloadBackup(serverType, labId, filename);
        if (file.Length > 0)
        {
            return File(file, "application/octet-stream", filename);
        }
        TempData["ErrorMessage"] = "Download failed.";
        return RedirectToAction("Index", "Backup");
    }
}