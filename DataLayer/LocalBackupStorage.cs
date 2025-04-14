namespace DataLayer;
/// <summary>
/// Class that handels local backup storage.
/// </summary>
public class LocalBackupStorage
{
    private readonly string _backupPath;
    
    public LocalBackupStorage()
    {
        _backupPath = Path.Combine(Directory.GetCurrentDirectory(), "backups");
    }

    /// <summary>
    /// Saves a backup to the local storage.
    /// </summary>
    /// <param name="serverType"> Type of the server, which is used for path </param>
    /// <param name="labId"> ID of a lab </param>
    /// <param name="fileContent"> File that needs to be saved </param>
    public async void SaveBackup(string serverType, string labId, byte[] fileContent)
    {
        string extension= serverType == "CML" ? ".yaml" : ".zip";
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"{serverType}-{labId}-{timestamp}{extension}";
        string filePath = Path.Combine(_backupPath, serverType, labId, fileName);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        await File.WriteAllBytesAsync(filePath, fileContent);
    }

    /// <summary>
    /// Retrieves a backup from the local storage.
    /// </summary>
    /// <param name="serverType"> Type of the server you want to retrieve backup for</param>
    /// <param name="labId"> ID of a lab you want to retrieve backup for</param>
    /// <param name="fileName"> Name of file backup </param>
    /// <returns></returns>
    public async Task<byte[]?> GetBackup(string serverType, string labId, string fileName)
    {
        string filePath = Path.Combine(_backupPath, serverType, labId, fileName);
        if (File.Exists(filePath))
        {
            return await File.ReadAllBytesAsync(filePath);
        }
        return null;
    }

    /// <summary>
    /// Deletes a backup from the local storage.
    /// </summary>
    /// <param name="serverType"> Type of the server you want to delete backup for </param>
    /// <param name="labId"> ID of a lab you want to delete backup for </param>
    /// <param name="fileName"> Name of file you want to delete </param>
    /// <returns></returns>
    public bool DeleteBackup(string serverType, string labId, string fileName)
    {
        string filePath = Path.Combine(_backupPath, serverType, labId, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return true;
        }
        return false;
    }


    /// <summary>
    /// Retrieves all backup records from the local storage.
    /// </summary>
    /// <returns> Returns list of all backups in /backup folder </returns>
    public List<BackupRecord> GetBackupRecords()
    {
        var records = new List<BackupRecord>();
        // Loop through all directories and files in the backup path
        foreach (var serverType in Directory.GetDirectories(_backupPath))
        {
            // Loop through all directories and files in the server type directory
            foreach (var labId in Directory.GetDirectories(serverType))
            {
                // Loop through all files in the lab ID directory
                foreach (var file in Directory.GetFiles(labId))
                {
                    var record = new BackupRecord
                    {
                        ServerType = serverType.StartsWith(_backupPath, StringComparison.OrdinalIgnoreCase) ? serverType.Substring(_backupPath.Length + 1) : serverType,
                        LabId = labId.StartsWith(_backupPath, StringComparison.OrdinalIgnoreCase) ? labId.Substring(serverType.Length + 1) : labId,
                        FileName = Path.GetFileName(file),
                        FullPath = file,
                        CreatedAt = File.GetCreationTime(file)
                    };
                    records.Add(record);
                }
            }
        }
        return records;
    }
}