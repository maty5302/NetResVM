using BusinessLayer.DTOs;
using BusinessLayer.Interface;
using BusinessLayer.Services.ApiCiscoServices;
using BusinessLayer.Services.ApiEVEServices;
using DataLayer;
using SimpleLogger;

namespace BusinessLayer.Services;

/// <summary>
/// This class is responsible for managing backups of labs.
/// </summary>
public class BackupService
{
    private readonly ApiCiscoLabService _apiCisco;
    private readonly ApiEVELabService _apiEve;
    private readonly LocalBackupStorage _localBackupStorage;
    private readonly ServerService _serverService;
    private readonly ILogger _logger;

    public BackupService()
    {
        _apiCisco = new ApiCiscoLabService();
        _apiEve = new ApiEVELabService();
        _localBackupStorage = new LocalBackupStorage();
        _serverService = new ServerService();
        _logger = FileLogger.Instance;
    }

    /// <summary>
    /// Asynchronously creates a backup of a lab for a specified server.
    /// </summary>
    /// <param name="serverId">The unique identifier (ID) of the server where the lab is hosted.</param>
    /// <param name="labId">The unique identifier (ID) of the lab to back up.</param>
    /// <param name="serverType">The type of the server (e.g., CML, EVE-NG, etc.).</param>
    /// <returns>
    /// A tuple containing:
    /// <c>backup</c> – <c>true</c> if the backup was successfully created; otherwise, <c>false</c>,
    /// <c>Message</c> – a string message providing additional details (e.g., success or error description).
    /// </returns>
    public async Task<(bool backup, string Message)> BackupLab(int serverId, string labId, string serverType)
    {
        if (serverType == "CML")
        {
            var file = await GetCMLLabFile(serverId, labId);
            if (file != null)
            {
                _localBackupStorage.SaveBackup(serverType, labId, file);
                return (true, "Backup successful");
            }

            _logger.LogError("Backup failed. File could not be fetched.");
            return (false, "Backup failed");
        }
        else if (serverType == "EVE")
        {
            var file = await GetEVELabFile(serverId, labId);
            if (file != null)
            {
                _localBackupStorage.SaveBackup(serverType, labId, file);
                return (true, "Backup successful");
            }

            _logger.LogError("Backup failed. File could not be fetched.");
            return (false, "Backup failed");
        }

        _logger.LogError("Backup failed. Server type not recognized.");
        return (false, "Not implemented");
    }

    /// <summary>
    /// Asynchronously retrieves the lab file for a specific lab on a CML server.
    /// </summary>
    /// <param name="serverId">The unique identifier (ID) of the server hosting the lab.</param>
    /// <param name="labId">The unique identifier (ID) of the lab to retrieve the file for.</param>
    /// <returns>
    /// A byte array representing the lab file if successfully retrieved; otherwise, <c>null</c> if the operation fails.
    /// </returns>
    private async Task<byte[]?> GetCMLLabFile(int serverId, string labId)
    {
        var response = await _apiCisco.DownloadLab(serverId, labId);
        if (response.fileContent != null)
        {
            return response.fileContent;
        }

        return null;
    }

    /// <summary>
    /// Asynchronously retrieves the lab file for a specific lab on a EVE server.
    /// </summary>
    /// <param name="serverId">The unique identifier (ID) of the server hosting the lab.</param>
    /// <param name="labId">The unique identifier (ID) of the lab to retrieve the file for.</param>
    /// <returns>
    /// A byte array representing the lab file if successfully retrieved; otherwise, <c>null</c> if the operation fails.
    /// </returns>
    private async Task<byte[]?> GetEVELabFile(int serverId, string labId)
    {
        var lab = await _apiEve.GetLabInfoById(serverId, labId);
        if (lab == null)
        {
            return null;
        }

        var response = await _apiEve.DownloadLab(lab, serverId);
        if (response != null)
        {
            return response;
        }

        return null;
    }

    /// <summary>
    /// Asynchronously retrieves a list of all backups available in the system.
    /// </summary>
    /// <returns>
    /// A list of <see cref="BackupDTO"/> objects representing the backups.
    /// Returns an empty list if no backups are found or <c>null</c> if an error occurs.
    /// </returns>
    public async Task<List<BackupDTO>> GetBackups()
    {
        var backups = _localBackupStorage.GetBackupRecords();
        var servers = _serverService.GetAllServers();
        var backupDTOs = new List<BackupDTO>();
        if (servers == null || backups == null)
        {
            return backupDTOs; // Return empty list if no servers or backups found
        }

        foreach (var backup in backups)
        {
            ServerDTO? s = null;
            // Find the server that matches the backup
            foreach (var server in servers)
            {
                ILabModel res;
                if (backup.ServerType == "CML")
                    res = (await _apiCisco.GetLabInfo(server.Id, backup.LabId)).lab;
                else
                    res = await _apiEve.GetLabInfoById(server.Id, backup.LabId);
                if (res != null)
                {
                    s = server;
                    break;
                }
            }

            if (s != null)
            {
                // Lab exists and has valid server = add it to the list
                backupDTOs.Add(new BackupDTO
                {
                    ServerId = s.Id,
                    ServerName = s.Name,
                    LabId = backup.LabId,
                    ServerType = backup.ServerType,
                    FileName = backup.FileName,
                    FullPath = backup.FullPath,
                    CreatedAt = backup.CreatedAt
                });
            }
            else
            {
                //move to no more existing lab
                backupDTOs.Add(new BackupDTO
                {
                    ServerId = -1,
                    ServerName = "Unknown",
                    LabId = backup.LabId,
                    ServerType = backup.ServerType,
                    FileName = backup.FileName,
                    FullPath = backup.FullPath,
                    CreatedAt = backup.CreatedAt
                });
            }
        }

        return backupDTOs;
    }

    /// <summary>
    /// Asynchronously restores a backup for a specified server and lab.
    /// </summary>
    /// <param name="serverId">The unique identifier (ID) of the server where the lab is hosted.</param>
    /// <param name="serverType">The type of the server (e.g., CML, EVE-NG).</param>
    /// <param name="labId">The unique identifier (ID) of the lab to restore the backup for.</param>
    /// <param name="fileName">The name of the backup file to restore.</param>
    /// <returns>
    /// <c>true</c> if the backup was successfully restored; otherwise, <c>false</c>.
    /// </returns>
    public async Task<bool> RestoreBackup(int serverId, string serverType, string labId, string fileName)
    {
        var file = await _localBackupStorage.GetBackup(serverType, labId, fileName);
        if (file != null)
        {
            if (serverType == "CML")
            {
                var res = await _apiCisco.ImportLab(serverId, file);
                if (res)
                {
                    _localBackupStorage.DeleteBackup(serverType, labId, fileName);
                    return true;
                }

                return false;
            }
            else if (serverType == "EVE")
            {
                return await _apiEve.ImportLab(serverId, file, fileName);
            }
        }

        return false;
    }

    /// <summary>
    /// Deletes a backup for a specified lab and server based on the backup file name.
    /// </summary>
    /// <param name="fileName">The name of the backup file to delete.</param>
    /// <param name="labId">The unique identifier (ID) of the lab associated with the backup.</param>
    /// <param name="serverType">The type of the server (e.g., CML, EVE-NG) where the backup is stored.</param>
    /// <returns>
    /// <c>true</c> if the backup was successfully deleted; otherwise, <c>false</c>.
    /// </returns>
    public bool DeleteBackup(string fileName, string labId, string serverType)
    {
        return _localBackupStorage.DeleteBackup(serverType, labId, fileName);
    }

    /// <summary>
    /// Asynchronously downloads a backup file for a specific lab and server.
    /// </summary>
    /// <param name="serverType">The type of the server (e.g., CML, EVE-NG, etc.) hosting the backup.</param>
    /// <param name="labId">The unique identifier (ID) of the lab associated with the backup.</param>
    /// <param name="fileName">The name of the backup file to download.</param>
    /// <returns>
    /// A byte array containing the backup file if successfully downloaded; otherwise, an empty byte array or <c>null</c> if an error occurs.
    /// </returns>
    public async Task<byte[]> DownloadBackup(string serverType, string labId, string fileName)
    {
        var file = await _localBackupStorage.GetBackup(serverType, labId, fileName);
        if (file != null)
        {
            return file;
        }

        return Array.Empty<byte>();
    }
}