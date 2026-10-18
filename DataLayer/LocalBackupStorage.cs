using DataLayer.Interface;

namespace DataLayer;
/// <summary>
/// Class that handels local backup storage.
/// </summary>
public class LocalBackupStorage : ILocalBackupStorage
{
    private readonly string _backupPath;
    
    public LocalBackupStorage() 
    {
        _backupPath = Path.Combine(Directory.GetCurrentDirectory(), "backups");
    }

    /// <summary>
    /// Validates and constructs a safe, canonical file path strictly within the backup root directory.
    /// Returns null if any path traversal or invalid input is detected.
    /// </summary>
    private string? GetSafeFilePath(string serverType, string labId, string fileName)
    {
        if (string.IsNullOrWhiteSpace(serverType) || string.IsNullOrWhiteSpace(labId) || string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        // Strip directory separators and traversal characters from user inputs
        string cleanServerType = Path.GetFileName(serverType.Trim());
        string cleanLabId = Path.GetFileName(labId.Trim());
        string cleanFileName = Path.GetFileName(fileName.Trim());

        if (string.IsNullOrEmpty(cleanServerType) || string.IsNullOrEmpty(cleanLabId) || string.IsNullOrEmpty(cleanFileName))
        {
            return null;
        }

        string fullBackupRoot = Path.GetFullPath(_backupPath);
        string fullPath = Path.GetFullPath(Path.Combine(fullBackupRoot, cleanServerType, cleanLabId, cleanFileName));

        // Ensure resulting canonical path is strictly inside the backup root directory
        if (!fullPath.StartsWith(fullBackupRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return fullPath;
    }

    /// <summary>
    /// Saves a backup to the local storage.
    /// </summary>
    /// <param name="serverType"> Type of the server, which is used for path </param>
    /// <param name="labId"> ID of a lab </param>
    /// <param name="fileContent"> File that needs to be saved </param>
    /// <param name="fileExtension"> File extension for the backup file </param>
    public async Task SaveBackup(string serverType, string labId, byte[] fileContent, string fileExtension)
    {
        string cleanServerType = Path.GetFileName(serverType.Trim());
        string cleanLabId = Path.GetFileName(labId.Trim());
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"{cleanServerType}-{cleanLabId}-{timestamp}{fileExtension}";

        string? filePath = GetSafeFilePath(cleanServerType, cleanLabId, fileName);
        if (filePath == null)
        {
            throw new ArgumentException("Invalid path parameters for backup.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? _backupPath);
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
        string? filePath = GetSafeFilePath(serverType, labId, fileName);
        if (filePath != null && File.Exists(filePath))
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
        string? filePath = GetSafeFilePath(serverType, labId, fileName);
        if (filePath != null && File.Exists(filePath))
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
        if (!Directory.Exists(_backupPath))
        {
            Directory.CreateDirectory(_backupPath);
            return records;
        }

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