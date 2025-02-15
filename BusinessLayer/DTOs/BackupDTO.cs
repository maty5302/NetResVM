namespace BusinessLayer.DTOs;

public class BackupDTO
{
    public int ServerId { get; set; }
    public string ServerName { get; set; }
    public string LabId { get; set; }
    public string ServerType { get; set; }
    public string FileName { get; set; }
    public string FullPath { get; set; }
    public DateTime CreatedAt { get; set; }
}