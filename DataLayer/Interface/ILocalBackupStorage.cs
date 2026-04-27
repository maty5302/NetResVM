namespace DataLayer.Interface;

public interface ILocalBackupStorage
{
    void SaveBackup(string serverType, string labId, byte[] fileContent, string fileExtension);
    Task<byte[]?> GetBackup(string serverType, string labId, string fileName);
    bool DeleteBackup(string serverType, string labId, string fileName);
    
    List<BackupRecord> GetBackupRecords();
}