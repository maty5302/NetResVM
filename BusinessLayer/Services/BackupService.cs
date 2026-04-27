using System.Runtime.CompilerServices;
using BusinessLayer.DTOs;
using BusinessLayer.Enum;
using BusinessLayer.Extensions;
using BusinessLayer.Interface;
using DataLayer;
using DataLayer.Interface;
using SimpleLogger;

namespace BusinessLayer.Services;

/// <summary>
/// This class is responsible for managing backups of labs.
/// </summary>
public class BackupService
{
    private readonly ILocalBackupStorage _localBackupStorage;
    private readonly IServerService _serverService;
    private readonly IPlatformManager _platformManager;
    private readonly ILogger _logger;

    public BackupService(IPlatformManager platformManager, IServerService serverService, ILocalBackupStorage localBackupStorage)
    {
        _platformManager = platformManager;
        _localBackupStorage = localBackupStorage;
        _serverService = serverService;
        _logger = FileLogger.Instance;
    }

    /// <summary>
    /// Asynchronously creates a backup of a lab for a specified server.
    /// </summary>
    /// <param name="serverId">The unique identifier (ID) of the server where the lab is hosted.</param>
    /// <param name="labId">The unique identifier (ID) of the lab to back up.</param>
    /// <returns>
    /// A tuple containing:
    /// <c>backup</c> � <c>true</c> if the backup was successfully created; otherwise, <c>false</c>,
    /// <c>Message</c> � a string message providing additional details (e.g., success or error description).
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
        var settings = PlatformTypeExtensions.GetSettings(server.Platform);

        var labInfo = await adapter.GetLabInfoAsync(serverId,labId);
        if(labInfo.Lab == null)
        {
            _logger.LogError($"Backup failed. Lab with ID {labId} not found on server {serverId}.");
            return (false, "Lab not found");
        }
        var response = await adapter.DownloadLab(serverId, labId, labInfo.Lab);
        if(response.FileContent != null)
        {
            _localBackupStorage.SaveBackup(server.Platform.ToString(), labId, response.FileContent, settings.FileExtension);
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
            System.Enum.TryParse<PlatformType>(backup.ServerType, true, out var platformType);
            foreach (var server in servers)
            {                
                if (server.Platform != platformType)
                    continue;

                try
                {
                    var adapter = _platformManager.GetAdapter(server.Platform);
                    var authResult = await adapter.AuthenticateAsync(server.Id);
                    if (!authResult.Valid)
                    {
                        _logger.LogWarning($"BackupService - Authentication failed for server {server.Name}: {authResult.Message}");
                        continue; // Zkus�me dal�� server v seznamu
                    }

                    // 3. Dotaz na laborato� pomoc� sjednocen�ho rozhran�
                    var labResult = await adapter.GetLabInfoAsync(server.Id, backup.LabId);

                    // 4. Pokud n�m adapt�r vr�til laborato� (nen� null), na�li jsme spr�vn� server!
                    if (labResult.Lab != null)
                    {
                        matchingServer = server;
                        break; // Ukon��me prohled�v�n� server� pro tuto konkr�tn� z�lohu
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"BackupService - Error checking lab {backup.LabId} on server {server.Name}: {ex.Message}");
                }
            }

            if (matchingServer != null)
            {
                // Laborato� na serveru existuje = p�id�me validn� z�znam
                backupDTOs.Add(new BackupDTO
                {
                    ServerId = matchingServer.Id,
                    ServerName = matchingServer.Name,
                    LabId = backup.LabId,
                    Platform = matchingServer.Platform,
                    FileName = backup.FileName,
                    FullPath = backup.FullPath,
                    CreatedAt = backup.CreatedAt
                });
            }
            else
            {
                // Laborato� u� na ��dn�m zn�m�m serveru neexistuje (Unknown server)
                backupDTOs.Add(new BackupDTO
                {
                    ServerId = -1,
                    ServerName = "Unknown",
                    LabId = backup.LabId,
                    Platform = platformType,
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
    /// <param name="platform">The type of the server (e.g., CML, EVE-NG).</param>
    /// <param name="labId">The unique identifier (ID) of the lab to restore the backup for.</param>
    /// <param name="fileName">The name of the backup file to restore.</param>
    /// <returns>
    /// <c>true</c> if the backup was successfully restored; otherwise, <c>false</c>.
    /// </returns>
    public async Task<bool> RestoreBackup(int serverId, PlatformType platform, string labId, string fileName)
    {
        try
        {
            var file = await _localBackupStorage.GetBackup(platform.ToString(), labId, fileName);
            if (file == null)
            {
                _logger.LogWarning($"BackupService - RestoreBackup: Backup file {fileName} not found.");
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
                _localBackupStorage.DeleteBackup(platform.ToString(), labId, fileName);

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
    /// <param name="platform">The type of the server (e.g., CML, EVE-NG) where the backup is stored.</param>
    /// <returns>
    /// <c>true</c> if the backup was successfully deleted; otherwise, <c>false</c>.
    /// </returns>
    public bool DeleteBackup(string fileName, string labId, PlatformType platform)
    {
        return _localBackupStorage.DeleteBackup(platform.ToString(), labId, fileName);
    }

    /// <summary>
    /// Asynchronously downloads a backup file for a specific lab and server.
    /// </summary>
    /// <param name="platform">The type of the server (e.g., CML, EVE-NG, etc.) hosting the backup.</param>
    /// <param name="labId">The unique identifier (ID) of the lab associated with the backup.</param>
    /// <param name="fileName">The name of the backup file to download.</param>
    /// <returns>
    /// A byte array containing the backup file if successfully downloaded; otherwise, an empty byte array or <c>null</c> if an error occurs.
    /// </returns>
    public async Task<byte[]> DownloadBackup(PlatformType platform, string labId, string fileName)
    {
        var file = await _localBackupStorage.GetBackup(platform.ToString(), labId, fileName);
        if (file != null)
        {
            return file;
        }

        return Array.Empty<byte>();
    }
}