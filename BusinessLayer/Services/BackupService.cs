using ApiCisco;
using BusinessLayer.DTOs;
using BusinessLayer.Interface;
using BusinessLayer.Services.ApiCiscoServices;
using BusinessLayer.Services.ApiEVEServices;
using DataLayer;
using SimpleLogger;

namespace BusinessLayer.Services;

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


    private async Task<byte[]?> GetCMLLabFile(int serverId, string labId)
    {
        var response = await _apiCisco.DownloadLab(serverId, labId);
        if (response.fileContent != null)
        {
            return response.fileContent;
        }

        return null;
    }

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


    public async Task<List<BackupDTO>> GetBackups()
    {
        var backups = _localBackupStorage.GetBackupRecords();
        var servers = _serverService.GetAllServers();
        var backupDTOs = new List<BackupDTO>();

        foreach (var backup in backups)
        {
            ServerDTO? s = null;

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

    public bool DeleteBackup(string fileName, string labId, string serverType)
    {
        return _localBackupStorage.DeleteBackup(serverType, labId, fileName);
    }

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