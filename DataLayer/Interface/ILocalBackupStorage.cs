namespace DataLayer.Interface;

/// <summary>
/// Defines methods for saving, retrieving, and deleting backup files in a local storage system, as well as retrieving
/// metadata about available backups.
/// </summary>
/// <remarks>Implementations of this interface provide local backup management functionality for different server
/// types and lab environments. Methods support storing backup files with associated metadata and retrieving or deleting
/// them as needed. Thread safety and file system access considerations depend on the specific implementation.</remarks>
public interface ILocalBackupStorage
{
    /// <summary>
    /// Saves a backup file for the specified lab and server type using the provided file content and extension.
    /// </summary>
    /// <param name="serverType">The type of server for which the backup is being saved. This value determines the backup destination or handling
    /// logic.</param>
    /// <param name="labId">The unique identifier of the lab associated with the backup. Cannot be null or empty.</param>
    /// <param name="fileContent">The binary content of the backup file to be saved. Cannot be null.</param>
    /// <param name="fileExtension">The file extension indicating the format of the backup file (for example, ".zip" or ".yml"). Cannot be null or
    /// empty.</param>
    void SaveBackup(string serverType, string labId, byte[] fileContent, string fileExtension);

    /// <summary>
    /// Asynchronously retrieves the backup file for the specified server type, lab, and file name.
    /// </summary>
    /// <param name="serverType">The type of server from which to retrieve the backup. This value determines the backup source.</param>
    /// <param name="labId">The unique identifier of the lab associated with the backup file.</param>
    /// <param name="fileName">The name of the backup file to retrieve. Must match the exact file name stored on the server.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a byte array with the backup file
    /// contents, or null if the file does not exist.</returns>
    Task<byte[]?> GetBackup(string serverType, string labId, string fileName);

    /// <summary>
    /// Deletes the specified backup file for a given server type and laboratory identifier.
    /// </summary>
    /// <param name="serverType">The type of server from which the backup should be deleted. Cannot be null or empty.</param>
    /// <param name="labId">The unique identifier of the laboratory associated with the backup. Cannot be null or empty.</param>
    /// <param name="fileName">The name of the backup file to delete. Cannot be null or empty.</param>
    /// <returns>true if the backup file was successfully deleted; otherwise, false.</returns>
    bool DeleteBackup(string serverType, string labId, string fileName);
    
    /// <summary>
    /// Retrieves a list of all available backup records.
    /// </summary>
    /// <returns>A list of <see cref="BackupRecord"/> objects representing the available backups. The list is empty if no backups
    /// are found.</returns>
    List<BackupRecord> GetBackupRecords();
}