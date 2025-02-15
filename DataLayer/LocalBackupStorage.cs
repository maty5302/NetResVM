namespace DataLayer;

public class LocalBackupStorage
{
    private readonly string _backupPath;
    
    public LocalBackupStorage()
    {
        _backupPath = Path.Combine(Directory.GetCurrentDirectory(), "backups");
    }

    public async void SaveBackup(string serverType, string labId, byte[] fileContent)
    {
        string extension= serverType == "CML" ? ".yaml" : ".zip";
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"{serverType}-{labId}-{timestamp}{extension}";
        string filePath = Path.Combine(_backupPath, serverType, labId, fileName);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        await File.WriteAllBytesAsync(filePath, fileContent);
    }

    public async Task<byte[]?> GetBackup(string serverType, string labId, string fileName)
    {
        string filePath = Path.Combine(_backupPath, serverType, labId, fileName);
        if (File.Exists(filePath))
        {
            return await File.ReadAllBytesAsync(filePath);
        }
        return null;
    }
    
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

    public List<BackupRecord> GetBackupRecords()
    {
        var records = new List<BackupRecord>();
        foreach (var serverType in Directory.GetDirectories(_backupPath))
        {
            foreach (var labId in Directory.GetDirectories(serverType))
            {
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