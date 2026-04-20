using BusinessLayer.DTOs;
using BusinessLayer.Enum;
using BusinessLayer.Interface;
using DataLayer;
using SimpleLogger;

namespace BusinessLayer.Services;

/// <summary>
/// This class is responsible for managing backups of labs.
/// </summary>
public class BackupService
{
    private readonly LocalBackupStorage _localBackupStorage;
    private readonly ServerService _serverService;
    private readonly PlatformManager _platformManager;
    private readonly ILogger _logger;

    public BackupService(PlatformManager platformManager)
    {
        _platformManager = platformManager; 
        _localBackupStorage = new LocalBackupStorage();
        _serverService = new ServerService();
        _logger = FileLogger.Instance;
    }

    /// <summary>
    /// Asynchronously creates a backup of a lab for a specified server.
    /// </summary>
    /// <param name="serverId">The unique identifier (ID) of the server where the lab is hosted.</param>
    /// <param name="labId">The unique identifier (ID) of the lab to back up.</param>
    /// <returns>
    /// A tuple containing:
    /// <c>backup</c> – <c>true</c> if the backup was successfully created; otherwise, <c>false</c>,
    /// <c>Message</c> – a string message providing additional details (e.g., success or error description).
    /// </returns>
    public async Task<(bool backup, string Message)> BackupLab(int serverId, string labId)
    {
        var server = _serverService.GetServerById(serverId);
        if (server == null)
        {
            _logger.LogError($"Backup failed. Server with ID {serverId} not found.");
            return (false, "Server not found");
        }
        if(server.Platform==Enum.PlatformType.Unknown)
        {
            _logger.LogError($"Backup failed. Server with ID {serverId} has an unknown platform."); 
            return (false, "Unknown platform");
        }
        IVirtualizationAdapter adapter = _platformManager.GetAdapter(server.Platform);
        
        var labInfo = await adapter.GetLabInfoAsync(serverId,labId);
        if(labInfo.Lab == null)
        {
            _logger.LogError($"Backup failed. Lab with ID {labId} not found on server {serverId}.");
            return (false, "Lab not found");
        }
        var response = await adapter.DownloadLab(serverId, labId, labInfo.Lab);
        if(response.FileContent != null)
        {
            _localBackupStorage.SaveBackup(server.ServerType, labId, response.FileContent);
            return (true, "Backup successful");
        }
        else
        {
            _logger.LogError("Backup failed. File could not be fetched.");
            return (false, "Backup failed");
        }
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
        if (servers == null || backups == null || !backups.Any())
        {
            return backupDTOs; // Return empty list if no servers or backups found
        }


        foreach (var backup in backups)
        {
            ServerDTO? matchingServer = null;
            // Find the server that matches the backup
            foreach (var server in servers)
            {
                System.Enum.TryParse<PlatformType>(backup.ServerType, true, out var platformType);
                if (server.Platform != platformType)
                    continue;

                try
                {
                    var adapter = _platformManager.GetAdapter(server.Platform);
                    var authResult = await adapter.AuthenticateAsync(server.Id);
                    if (!authResult.Valid)
                    {
                        _logger.LogWarning($"BackupService - Authentication failed for server {server.Name}: {authResult.Message}");
                        continue; // Zkusíme další server v seznamu
                    }

                    // 3. Dotaz na laboratoø pomocí sjednoceného rozhraní
                    var labResult = await adapter.GetLabInfoAsync(server.Id, backup.LabId);

                    // 4. Pokud nám adaptér vrátil laboratoø (není null), našli jsme správný server!
                    if (labResult.Lab != null)
                    {
                        matchingServer = server;
                        break; // Ukonèíme prohledávání serverù pro tuto konkrétní zálohu
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"BackupService - Error checking lab {backup.LabId} on server {server.Name}: {ex.Message}");
                }
            }

            if (matchingServer != null)
            {
                // Laboratoø na serveru existuje = pøidáme validní záznam
                backupDTOs.Add(new BackupDTO
                {
                    ServerId = matchingServer.Id,
                    ServerName = matchingServer.Name,
                    LabId = backup.LabId,
                    ServerType = backup.ServerType,
                    FileName = backup.FileName,
                    FullPath = backup.FullPath,
                    CreatedAt = backup.CreatedAt
                });
            }
            else
            {
                // Laboratoø už na žádném známém serveru neexistuje (Unknown server)
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


    //public async Task<bool> RestoreBackup(int serverId, string serverType, string labId, string fileName)
    //{
    //    var file = await _localBackupStorage.GetBackup(serverType, labId, fileName);
    //    if (file != null)
    //    {
    //        if (serverType == "CML")
    //        {
    //            var res = await _apiCisco.ImportLab(serverId, file);
    //            if (res)
    //            {
    //                _localBackupStorage.DeleteBackup(serverType, labId, fileName);
    //                return true;
    //            }

    //            return false;
    //        }
    //        else if (serverType == "EVE")
    //        {
    //            return await _apiEve.ImportLab(serverId, file, fileName);
    //        }
    //    }

    //    return false;
    //}

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
        try
        {
            var file = await _localBackupStorage.GetBackup(serverType, labId, fileName);
            if (file == null)
            {
                _logger.LogWarning($"BackupService - RestoreBackup: Backup file {fileName} not found.");
                return false;
            }

            if (!System.Enum.TryParse<PlatformType>(serverType, out var platform))
            {
                _logger.LogError($"BackupService - RestoreBackup: Unsupported platform type '{serverType}'.");
                return false;
            }

            var adapter = _platformManager.GetAdapter(platform);
            var authResult = await adapter.AuthenticateAsync(serverId);

            if (!authResult.Valid)
            {
                _logger.LogError($"BackupService - RestoreBackup: Auth failed for server {serverId}. {authResult.Message}");
                return false;
            }

            var importResult = await adapter.ImportLab(serverId, file, fileName); 

            if (importResult)
            {
                _localBackupStorage.DeleteBackup(serverType, labId, fileName);

                _logger.Log($"BackupService - RestoreBackup: Successfully restored {fileName} to server {serverId}.");
                return true;
            }
            else
            {
                _logger.LogError($"BackupService - RestoreBackup: Adapter failed to import.");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"BackupService - RestoreBackup: Exception occurred - {ex.Message}");
            return false;
        }
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