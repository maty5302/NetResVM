namespace BusinessLayer.DTOs;

/// <summary>
/// Data Transfer Object for a backup, containing information about the server, lab, 
/// and the backup file itself, including the file's creation date.
/// </summary>
public class BackupDTO
{
    /// <summary>
    /// Gets or sets the ID of the server where the backup was created.
    /// </summary>
    public int ServerId { get; set; }

    /// <summary>
    /// Gets or sets the name of the server where the backup was created.
    /// </summary>
    public string ServerName { get; set; }

    /// <summary>
    /// Gets or sets the ID of the lab for which the backup was created.
    /// </summary>
    public string LabId { get; set; }

    /// <summary>
    /// Gets or sets the type of server (e.g., Cisco, EVE, etc.).
    /// </summary>
    public string ServerType { get; set; }

    /// <summary>
    /// Gets or sets the name of the backup file.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Gets or sets the full path to the backup file.
    /// </summary>
    public string FullPath { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the backup was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
